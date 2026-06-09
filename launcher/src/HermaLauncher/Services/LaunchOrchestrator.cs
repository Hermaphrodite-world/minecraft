using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// 구현계획 §4 실행 순서 불변식의 단일 진입점. 각 단계는 명시적 실패 게이트(try/catch)
// 로 감싸 비개발자에게 한국어 메시지를 보여준다(Codex H1).
public sealed class LaunchOrchestrator
{
    private readonly IUpdateService _update;
    private readonly IAuthService _auth;
    private readonly IMinecraftService _minecraft;
    private readonly PackwizService _packwiz;

    public LaunchOrchestrator(
        IUpdateService update,
        IAuthService auth,
        IMinecraftService minecraft,
        PackwizService packwiz)
    {
        _update = update;
        _auth = auth;
        _minecraft = minecraft;
        _packwiz = packwiz;
    }

    // 편의 생성자(기본 구현 연결).
    public LaunchOrchestrator()
        : this(new VelopackUpdateService(), new CmlLibAuthService(), new CmlLibMinecraftService(), new PackwizService())
    {
    }

    // 성공 시 true. 실패 시 progress 로 Error 보고 후 false.
    public async Task<bool> RunAsync(LaunchOptions options, IProgress<StageUpdate> progress, CancellationToken ct)
    {
        try
        {
            // (1) 자체 업데이트 — 소스 부재/오류는 graceful skip(예외 안 던짐).
            var restarting = await _update.CheckAndApplyAsync(progress, ct).ConfigureAwait(false);
            if (restarting)
                return true; // 업데이트 적용 위해 재시작

            ct.ThrowIfCancellationRequested();

            // (2) 인증 (offline username 또는 online device-code)
            var session = await _auth.AuthenticateAsync(options, progress, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            // (3) Java 설치/검증 -> 경로 캐싱  ★ (4)보다 먼저 (닭/달걀 불변식)
            var javaPath = await _minecraft.EnsureJavaAsync(progress, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            // (4) packwiz 동기화 — (3)에서 얻은 java 재사용. bootstrap jar 는 PackwizService 가 자동 확보.
            await _packwiz.RunAsync(javaPath, LauncherConfig.PackTomlUrl, progress, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            // (5)+(6) Fabric 설치 + ServerIp 주입 실행 (토큰은 직전 인증에서 확보)
            await _minecraft.LaunchAsync(session, progress, ct).ConfigureAwait(false);

            progress.Report(StageUpdate.Of(LaunchStage.Running, "게임을 실행했어요. 즐겜!", 1.0));
            return true;
        }
        catch (OperationCanceledException)
        {
            progress.Report(StageUpdate.Of(LaunchStage.Idle, "취소했어요."));
            return false;
        }
        catch (LaunchStageException ex)
        {
            // 단계별 사용자 친화 메시지
            progress.Report(StageUpdate.Error(ex.Stage, ex.Message));
            return false;
        }
        catch (Exception ex)
        {
            progress.Report(StageUpdate.Error(LaunchStage.Failed,
                "알 수 없는 오류가 발생했어요. 다시 시도해 주세요.\n" + ex.Message));
            return false;
        }
    }
}
