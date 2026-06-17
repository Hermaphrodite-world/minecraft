using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// 자동접속 대상 서버 endpoint(host:port)를 "한 곳에서" 해석한다.
//   ★ 핵심: quickPlay 인자와 servers.dat 서버목록 등록이 반드시 같은 주소를 쓰도록 단일 SoT 로 묶는다.
//     (이전 버그: quickPlay 는 override(LAN IP), servers.dat 는 LauncherConfig.ServerIp(공개 IP) → 불일치 →
//      같은 LAN 의 다른 PC 클라가 서버목록 항목으론 못 닿고 Direct Connect 로만 접속되던 문제.)
//   ★ 진단성: 결정 분기·probe 결과·지연시간을 풍부히 로깅해, quickPlay 가 실패해도 launcher 로그만으로
//     "런처는 닿았는데 quickPlay 만 실패(=클라/타이밍)" vs "런처도 못 닿음(=네트워크/서버)" 를 구분 가능하게.
public readonly record struct ServerEndpoint(
    string Host,
    int Port,
    ServerHostResolver.Source Source,
    bool TcpReachable,
    long ProbeMs)
{
    public string Address => $"{Host}:{Port}";

    // 방어용 안전값 — 해석이 통째로 실패해도 런치를 막지 않도록 공개 IP 로 폴백.
    public static ServerEndpoint PublicFallback =>
        new(LauncherConfig.ServerIp, LauncherConfig.ServerPort, ServerHostResolver.Source.Public, false, -1);
}

public static class ServerEndpointResolver
{
    // 자동접속 endpoint 해석 + 진단 로깅. 절대 throw 하지 않는다(취소 제외) — 어떤 실패든 공개 IP 폴백.
    public static async Task<ServerEndpoint> ResolveAsync(IProgress<StageUpdate>? progress, CancellationToken ct)
    {
        try
        {
            return await ResolveCoreAsync(progress, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // 사용자 취소는 전파
        }
        catch (Exception ex)
        {
            // 방어: 예상 못 한 해석 실패는 런치를 막지 않고 공개 IP 로 진행.
            AppLog.Warn(LaunchStage.Launch, $"[endpoint] 해석 중 예외 → 공개 IP 폴백: {ex.GetType().Name}: {ex.Message}");
            return ServerEndpoint.PublicFallback;
        }
    }

    private static async Task<ServerEndpoint> ResolveCoreAsync(IProgress<StageUpdate>? progress, CancellationToken ct)
    {
        var port = LauncherConfig.ServerPort;

        // (a) override 읽기 — 설정 로드 실패도 방어(공개 IP 진행).
        string? rawOverride = null;
        try { rawOverride = LauncherSettings.Load().ServerHostOverride; }
        catch (Exception ex) { AppLog.Warn(LaunchStage.Launch, $"[endpoint] 설정 로드 실패(override 무시): {ex.Message}"); }

        var overrideHost = ServerHostResolver.Normalize(rawOverride);
        AppLog.Info(LaunchStage.Launch,
            $"[endpoint] 1/4 override 설정 raw='{rawOverride ?? "(없음)"}' → 정규화='{overrideHost ?? "(없음)"}'");

        // (b) 로컬 서버 감지 — override 가 명시 선택이라 최우선이므로, override 있으면 로컬 probe 생략.
        var localUp = false;
        if (overrideHost is null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                localUp = await ServerPing.IsServerUpAsync(ServerHostResolver.LoopbackHost, port, ct, timeoutMs: 700)
                                          .ConfigureAwait(false);
                sw.Stop();
                AppLog.Info(LaunchStage.Launch,
                    $"[endpoint] 2/4 로컬 서버 감지 {ServerHostResolver.LoopbackHost}:{port} = {(localUp ? "감지됨" : "없음")} ({sw.ElapsedMilliseconds}ms)");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                sw.Stop();
                AppLog.Warn(LaunchStage.Launch, $"[endpoint] 2/4 로컬 probe 오류(없음 처리): {ex.GetType().Name}: {ex.Message}");
            }
        }
        else
        {
            AppLog.Info(LaunchStage.Launch, "[endpoint] 2/4 override 지정됨 → 로컬 probe 생략(명시 선택 최우선)");
        }

        // (c) 결정 — override → 로컬 → 공개 (순수 함수, 단위 테스트됨).
        var source = ServerHostResolver.Decide(overrideHost, localUp);
        var host = source switch
        {
            ServerHostResolver.Source.UserOverride => overrideHost!,
            ServerHostResolver.Source.Local => ServerHostResolver.LoopbackHost,
            _ => LauncherConfig.ServerIp,
        };

        // 방어: 어떤 이유로든 host 가 비면 공개 IP 로.
        if (string.IsNullOrWhiteSpace(host))
        {
            AppLog.Warn(LaunchStage.Launch, $"[endpoint] 3/4 해석 host 가 비어있음(source={source}) → 공개 IP 폴백");
            host = LauncherConfig.ServerIp;
            source = ServerHostResolver.Source.Public;
        }
        AppLog.Info(LaunchStage.Launch, $"[endpoint] 3/4 결정: source={source}, host={host}:{port}");

        // (d) 도달성 probe — 순수 TCP connect(quickPlay 의 getsockopt 와 동일 레벨). 결정엔 영향 X, 진단 전용.
        //     런처가 여기서 connect 성공하는데 quickPlay 만 timeout 이면 클라/타이밍, 둘 다 실패면 네트워크/서버.
        var (reachable, probeMs, detail) = await TcpReachableAsync(host, port, ct).ConfigureAwait(false);
        AppLog.Info(LaunchStage.Launch,
            $"[endpoint] 4/4 TCP connect 진단 {host}:{port} = {(reachable ? "성공" : "실패")} ({probeMs}ms){detail}");

        // 사용자 표시 메시지(기존 톤 유지) + 미도달이어도 그래도 실행(원클릭 약속).
        progress?.Report(StageUpdate.Of(LaunchStage.Launch, source switch
        {
            ServerHostResolver.Source.UserOverride => reachable
                ? $"설정한 서버 주소로 접속합니다: {host}"
                : $"설정한 서버 주소로 접속합니다: {host} (응답 확인은 안 됐어요 — 그래도 시도해요)",
            ServerHostResolver.Source.Local => "이 PC에서 서버를 감지했어요 — 로컬(127.0.0.1)로 접속합니다.",
            _ => reachable
                ? "서버로 접속합니다."
                : "서버 응답을 확인 못 했어요(꺼져 있거나 점검 중일 수 있어요). 같은 집·네트워크에서 서버를 켰다면 설정의 '서버 주소 직접 입력'에 서버 PC의 IP를 넣어 주세요. 그래도 게임은 실행할게요.",
        }));

        if (!reachable)
            AppLog.Warn(LaunchStage.Launch,
                $"[endpoint] 선택 host 미도달(그래도 실행): {host}:{port} — quickPlay 가 실패하면 위 진단줄과 game-*.log 의 'Connection timed out/getsockopt' 를 대조하세요.");

        return new ServerEndpoint(host, port, source, reachable, probeMs);
    }

    // 순수 TCP connect 도달성 — SLP 핸드셰이크 없이 "포트가 connect 를 받아주나"만 측정. quickPlay 실패 원인 좁히기용.
    private static async Task<(bool ok, long ms, string detail)> TcpReachableAsync(string host, int port, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(2500);
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            sw.Stop();
            return (true, sw.ElapsedMilliseconds, "");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { sw.Stop(); return (false, sw.ElapsedMilliseconds, " — connect 타임아웃(2.5s)"); }
        catch (SocketException ex) { sw.Stop(); return (false, sw.ElapsedMilliseconds, $" — Socket {ex.SocketErrorCode}"); }
        catch (Exception ex) { sw.Stop(); return (false, sw.ElapsedMilliseconds, $" — {ex.GetType().Name}: {ex.Message}"); }
    }
}
