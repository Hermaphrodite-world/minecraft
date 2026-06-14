using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using Velopack;
using Velopack.Sources;
using XboxAuthNet.Game.Accounts;
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.Msal;

namespace HermaLauncher.Services;

// SynchronizationContext 마샬링 없이 무시하는 진행률 싱크(고빈도 byte progress UI flood 방지 — Codex).
internal sealed class NullProgress<T> : IProgress<T>
{
    public static readonly NullProgress<T> Instance = new();
    public void Report(T value) { }
}

// CmlLib.Core 4.0.6 / Auth.Microsoft 3.3.1 / Velopack 1.2.0 실제 통합.
// 모든 API 는 복원된 어셈블리 리플렉션으로 시그니처 검증함(docs/launcher-integration-notes.md).

// (2) 인증.
//  Offline=true  : MS 로그인 없이 username (online-mode=false 친구 서버 — WebView/Azure 불필요, 즉시 동작).
//  Offline=false : device-code MS 로그인 (online-mode=true 서버 — Azure 앱 client ID 필요, WebView 불필요).
public sealed class CmlLibAuthService : IAuthService
{
    private static readonly HttpClient _authHttp = new();

    public async Task<AuthSession> AuthenticateAsync(LaunchOptions options, IProgress<StageUpdate> progress, CancellationToken ct)
    {
        // ── 오프라인: username 만으로 진행 ──
        if (options.Offline)
        {
            var name = string.IsNullOrWhiteSpace(options.Username) ? "Player" : options.Username.Trim();
            progress.Report(StageUpdate.Of(LaunchStage.Auth, $"오프라인 모드: {name}", 1.0));
            return new AuthSession(name, string.Empty, "0", IsOffline: true);
        }

        // ── 온라인: silent(캐시 토큰) 우선 → 실패 시 시스템 브라우저 로그인(P1-1) ──
        if (!LauncherConfig.IsAzureClientConfigured)
            throw new LaunchStageException(LaunchStage.Auth,
                "온라인 로그인 설정이 빠졌어요. 런처가 올바른 로그인 설정으로 빌드돼야 해요 — 관리자에게 문의하거나 최신 런처로 업데이트해 주세요."); // P1-2: 제거된 오프라인 모드 지시 삭제

        var app = Microsoft.Identity.Client.PublicClientApplicationBuilder
            .Create(LauncherConfig.AzureClientId)
            .WithAuthority("https://login.microsoftonline.com/consumers")
            .WithRedirectUri("http://localhost") // 시스템 브라우저 loopback (random port)
            .Build();
        var accountManager = new JsonXboxGameAccountManager(AppPaths.AccountsJson);

        // (1) silent 우선 — 캐시된 계정으로 브라우저 없이 재로그인. 실패하면 (2) 브라우저.
        var cached = accountManager.GetDefaultAccount();
        if (cached is not null)
        {
            progress.Report(StageUpdate.Of(LaunchStage.Auth, "로그인 정보 확인 중…"));
            var silentSession = await TrySilentAsync(app, accountManager, ct).ConfigureAwait(false);
            if (silentSession is not null)
            {
                AppLog.Info(LaunchStage.Auth, "silent 로그인 성공(브라우저 생략)");
                return ToAuthSession(silentSession);
            }
            AppLog.Info(LaunchStage.Auth, "silent 로그인 불가(토큰 만료/부재) → 브라우저 로그인");
        }

        // (2) 브라우저(interactive) — silent 실패 또는 첫 로그인.
        progress.Report(StageUpdate.Of(LaunchStage.Auth,
            "기본 브라우저에서 Microsoft 로그인을 진행해 주세요. 완료되면 자동으로 이어집니다."));
        try
        {
            var account = cached ?? accountManager.NewAccount();
            var authenticator = new NestedAuthenticator
            {
                Context = new AuthenticateContext(account.SessionStorage, _authHttp, ct,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance),
            };
            authenticator.AddMsalOAuth(app, msal => msal.Interactive()); // 시스템 브라우저
            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
            authenticator.AddJEAuthenticator();

            MSession session = await authenticator.ExecuteForLauncherAsync().ConfigureAwait(false);
            SaveAccountsBestEffort(accountManager);
            return ToAuthSession(session);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (JEAuthException ex)
        {
            var msg = ex.StatusCode == 404
                ? "이 계정은 Minecraft: Java Edition 을 소유하고 있지 않아요. 구매 또는 계정을 확인해 주세요."
                : $"로그인 실패: {ex.ErrorMessage ?? ex.Message}";
            throw new LaunchStageException(LaunchStage.Auth, msg, ex);
        }
        catch (Exception ex)
        {
            var detail = ex is AggregateException agg
                ? string.Join(" / ", agg.Flatten().InnerExceptions.Select(e => e.Message))
                : ex.Message;
            throw new LaunchStageException(LaunchStage.Auth, "로그인에 실패했어요.\n" + detail, ex);
        }
    }

    // (5.5) SessionRefresh — proc.Start 직전 세션 재검증. 긴 설치 중 토큰 만료 대응(P1-1).
    // best-effort: silent refresh 성공하면 새 세션, 실패/오프라인이면 기존 세션 유지(런치 비차단).
    public async Task<AuthSession> RevalidateAsync(AuthSession current, IProgress<StageUpdate> progress, CancellationToken ct)
    {
        if (current.IsOffline || !LauncherConfig.IsAzureClientConfigured)
            return current;
        try
        {
            var app = Microsoft.Identity.Client.PublicClientApplicationBuilder
                .Create(LauncherConfig.AzureClientId)
                .WithAuthority("https://login.microsoftonline.com/consumers")
                .WithRedirectUri("http://localhost").Build();
            var accountManager = new JsonXboxGameAccountManager(AppPaths.AccountsJson);
            var s = await TrySilentAsync(app, accountManager, ct).ConfigureAwait(false);
            if (s is not null)
            {
                AppLog.Info(LaunchStage.SessionRefresh, "세션 재검증 완료(토큰 갱신)");
                return ToAuthSession(s);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { AppLog.Warn(LaunchStage.SessionRefresh, "세션 재검증 생략: " + ex.GetType().Name); }
        return current; // 갱신 실패 → 기존 세션으로 진행(대개 아직 유효)
    }

    // 캐시된 default 계정으로 silent(브라우저 없이) 인증 시도. 성공=MSession, 실패/계정없음=null.
    private static async Task<MSession?> TrySilentAsync(
        Microsoft.Identity.Client.IPublicClientApplication app,
        JsonXboxGameAccountManager accountManager, CancellationToken ct)
    {
        var account = accountManager.GetDefaultAccount();
        if (account is null) return null;
        try
        {
            var silent = new NestedAuthenticator
            {
                Context = new AuthenticateContext(account.SessionStorage, _authHttp, ct,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance),
            };
            silent.AddMsalOAuth(app, msal => msal.Silent());
            silent.AddXboxAuthForJE(xbox => xbox.Basic());
            silent.AddJEAuthenticator();
            var s = await silent.ExecuteForLauncherAsync().ConfigureAwait(false);
            SaveAccountsBestEffort(accountManager);
            return s;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return null; } // 토큰 만료/부재 등 → interactive 로 fallback
    }

    private static void SaveAccountsBestEffort(JsonXboxGameAccountManager mgr)
    {
        try
        {
            mgr.SaveAccounts();
            SecureFile.Harden(AppPaths.AccountsJson); // P2-3: 토큰 캐시 권한 강화
        }
        catch (Exception ex) { AppLog.Warn(LaunchStage.Auth, "계정 캐시 저장 실패(다음 실행 재로그인 필요할 수 있어요): " + ex.Message); }
    }

    private static AuthSession ToAuthSession(MSession s)
    {
        var session = new AuthSession(s.Username ?? string.Empty, s.UUID ?? string.Empty,
            s.AccessToken ?? string.Empty, IsOffline: false, Xuid: s.Xuid ?? string.Empty);
        AccountCache.Remember(session.Username); // P3-1: 로그인 표시명 캐시(시작/설정 화면용)
        return session;
    }
}

// (3)+(5)+(6) Fabric 설치 → 게임/Java 설치 → 실행. EnsureJavaAsync 가 무거운 설치를 수행하고
//   java 경로를 반환(packwiz 가 재사용), LaunchAsync 는 build+start 만.
public sealed class CmlLibMinecraftService : IMinecraftService
{
    private readonly HttpClient _http = new();
    private readonly MinecraftLauncher _launcher;

    // EnsureJavaAsync 가 쓰고 LaunchAsync 가 읽는다. 단일 Play 흐름 내에서 순차(await)이고,
    // 동시 Play 는 VM 의 PlayCommand CanExecute=!IsBusy 로 차단되므로 single-flight 보장(Codex #2).
    private string? _fabricVersionId;

    public CmlLibMinecraftService()
    {
        _launcher = new MinecraftLauncher(new MinecraftPath(AppPaths.GameDir));
    }

    public async Task<string> EnsureJavaAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        PreflightChecks.EnsureDiskSpace(AppPaths.GameDir, LaunchStage.Java); // P1-5: 무거운 설치 전 빠른 실패
        try
        {
            progress.Report(StageUpdate.Of(LaunchStage.Java, "Fabric 로더 설치 중…"));
            var fabric = new FabricInstaller(_http);
            // P2-2: loader 버전 핀(설정돼 있으면 3-arg, 비면 최신 자동).
            _fabricVersionId = string.IsNullOrWhiteSpace(LauncherConfig.FabricLoaderVersion)
                ? await fabric.Install(LauncherConfig.MinecraftVersion, _launcher.MinecraftPath).ConfigureAwait(false)
                : await fabric.Install(LauncherConfig.MinecraftVersion, LauncherConfig.FabricLoaderVersion, _launcher.MinecraftPath).ConfigureAwait(false);

            progress.Report(StageUpdate.Of(LaunchStage.Java, "게임 파일·Java 설치 중…"));
            var fileProgress = new Progress<InstallerProgressChangedEventArgs>(e =>
                progress.Report(StageUpdate.Of(LaunchStage.Java, e.Name ?? "설치 중",
                    e.TotalTasks > 0 ? (double)e.ProgressedTasks / e.TotalTasks : (double?)null)));
            // byte 진행률은 고빈도라 UI 마샬링 없이 무시(NullProgress) — flood 방지.
            await _launcher.InstallAsync(_fabricVersionId, fileProgress, NullProgress<ByteProgress>.Instance, ct)
                           .ConfigureAwait(false);

            var version = await _launcher.GetVersionAsync(_fabricVersionId, ct).ConfigureAwait(false);
            var javaPath = _launcher.GetJavaPath(version);
            if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
                javaPath = _launcher.GetDefaultJavaPath();
            if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
                throw new LaunchStageException(LaunchStage.Java,
                    "Java 런타임을 찾지 못했어요. 잠시 후 다시 시도해 주세요.");
            return javaPath!;
        }
        catch (OperationCanceledException) { throw; }
        catch (LaunchStageException) { throw; }
        catch (Exception ex)
        {
            // Fabric/게임 설치 중 네트워크 끊김·디스크 부족·서버 오류 등을 단계별 메시지로 변환(Codex Launch-R1)
            // — generic "알 수 없는 오류" 대신 사용자가 행동할 수 있는 안내.
            AppLog.Error(LaunchStage.Java, "게임 파일/Java 설치 실패: " + ex.Message);
            throw new LaunchStageException(LaunchStage.Java,
                "게임 파일·Java 설치 중 문제가 생겼어요. 네트워크와 디스크 여유 공간을 확인하고 다시 시도해 주세요.", ex);
        }
    }

    public async Task<Process> LaunchAsync(AuthSession session, IProgress<StageUpdate> progress, CancellationToken ct)
    {
        if (_fabricVersionId is null)
            throw new LaunchStageException(LaunchStage.Fabric, "설치 단계가 완료되지 않았어요.");

        progress.Report(StageUpdate.Of(LaunchStage.Launch, "게임 실행 준비 중…"));

        // 서버를 켠 본인 PC(호스트)는 NAT 헤어핀 미지원 시 자기 공개 IP 로 자기 서버에 못 들어간다.
        // → 접속 직전 127.0.0.1:port 짧은 TCP probe. 로컬에 서버가 떠 있으면 로컬로, 아니면 공개 ServerIp.
        var host = await ResolveServerHostAsync(progress, ct).ConfigureAwait(false);

        var option = new MLaunchOption
        {
            Session = ToMSession(session),
            MaximumRamMb = RamAdvisor.EffectiveMaxRamMb(), // P3-3: 호스트 RAM 기반(설정 override 우선)
            // ※ DockName 미설정. 공백 포함 값("Herma Launcher")을 macOS 에서 DockName 으로 주면 CmlLib 의
            //    런치 인자 구성에서 그 값이 메인 클래스 토큰으로 잘못 들어가 게임이 즉시 종료된다
            //    (macOS 실측: `java.lang.ClassNotFoundException: Herma Launcher`). Windows 는 DockName=null
            //    이라 영향 없음. dock 라벨보다 실행이 우선이므로 제거.
            // ★ MC 26.1 은 구형 --server/--port 인자를 제거함 → 모던 quickPlayMultiplayer 로 1-클릭 자동 접속.
            ExtraGameArguments = new[]
            {
                new MArgument("--quickPlayMultiplayer"),
                new MArgument($"{host}:{LauncherConfig.ServerPort}"),
            },
        };

        // 이미 EnsureJavaAsync 에서 설치 완료 → build only.
        var proc = await _launcher.BuildProcessAsync(_fabricVersionId, option, ct).ConfigureAwait(false);
        // 게임 stdout/stderr 를 game-*.log 로 캡처(P0/P1-7) — MC 자체 로그 init 전 조기 크래시 진단용(macOS 실사례).
        // redirect 설정 실패(UseShellExecute 충돌 등) 시 캡처 생략 — 런치 흐름 비차단.
        var captureAttached = TryEnableGameLogCapture(proc);
        try
        {
            ct.ThrowIfCancellationRequested(); // build 후 start 직전 마지막 취소 가드(취소했는데 게임이 뜨는 race 방지)
            if (!proc.Start())
                throw new LaunchStageException(LaunchStage.Launch, "게임 프로세스를 시작하지 못했어요.");
            if (captureAttached)
            {
                // redirect 를 켰으면 반드시 읽어야 파이프 버퍼가 안 막힘.
                try { proc.BeginOutputReadLine(); proc.BeginErrorReadLine(); } catch { /* 캡처 best-effort */ }
            }
            AppLog.Info(LaunchStage.Running, $"게임 실행 시작 (pid={proc.Id}, 캡처={captureAttached})");
            return proc; // 성공 — 핸들은 호출자(런처 모니터)가 소유/Dispose
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ObjectDisposedException)
        {
            proc.Dispose();
            throw new LaunchStageException(LaunchStage.Launch, "게임 실행에 실패했어요. 다시 시도해 주세요.", ex);
        }
        catch
        {
            proc.Dispose(); // 취소(start 전) / start 실패 등 — 핸들 정리 후 전파
            throw;
        }
    }

    // 게임 프로세스 stdout/stderr 를 game-*.log 로 redirect. 성공 시 true(호출자가 BeginReadLine).
    // 실패(예: UseShellExecute 충돌)면 false → 캡처 없이 런치 계속(비차단).
    private static bool TryEnableGameLogCapture(Process proc)
    {
        try
        {
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            proc.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            var writer = new StreamWriter(AppLog.NewGameLogPath(), append: false, new UTF8Encoding(false)) { AutoFlush = true };
            var sync = new object();
            void Append(string? line)
            {
                if (line is null) return;
                lock (sync) { try { writer.WriteLine(line); } catch { /* 게임 종료 직후 등 */ } }
            }
            proc.OutputDataReceived += (_, e) => Append(e.Data);
            proc.ErrorDataReceived += (_, e) => Append(e.Data);
            proc.EnableRaisingEvents = true;
            proc.Exited += (_, _) => { lock (sync) { try { writer.Flush(); writer.Dispose(); } catch { } } };
            return true;
        }
        catch
        {
            return false; // redirect 불가 → 캡처 생략
        }
    }

    private static MSession ToMSession(AuthSession s)
        => s.IsOffline
            ? MSession.CreateOfflineSession(s.Username)
            // online-mode 접속에 필요한 UserType/Xuid 보존(3-인자 ctor 은 손실).
            : new MSession
            {
                Username = s.Username,
                AccessToken = s.AccessToken,
                UUID = s.Uuid,
                UserType = "msa",
                Xuid = s.Xuid,
            };

    // quickPlay 자동 접속 대상 host 해석. 로컬(127.0.0.1)에 서버가 떠 있으면 로컬, 아니면 공개 ServerIp.
    //   호스트(서버 켠 PC)는 NAT 헤어핀 미지원 시 공개 IP 로 자기 자신을 못 들어가므로 로컬로 우회.
    //   일반 친구 PC 는 로컬에 서버가 없어 probe 실패 → 공개 IP(무영향).
    private static async Task<string> ResolveServerHostAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        // 호스트(서버 켠 PC) 우회: 로컬에 MC 서버가 떠 있으면 127.0.0.1. MC handshake 로 실제 MC 서버만 인정
        // (P1-10 — 비-MC 프로세스가 같은 포트 점유 시 false-positive 제거).
        if (await ServerPing.IsServerUpAsync("127.0.0.1", LauncherConfig.ServerPort, ct, timeoutMs: 700).ConfigureAwait(false))
        {
            progress.Report(StageUpdate.Of(LaunchStage.Launch, "이 PC에서 서버를 감지했어요 — 로컬(127.0.0.1)로 접속합니다."));
            return "127.0.0.1";
        }

        // 공개 서버 사전 점검(P1-10) — 다운이어도 차단하지 않고 경고만(원클릭 약속 유지).
        var host = LauncherConfig.ServerIp;
        try
        {
            if (!await ServerPing.IsServerUpAsync(host, LauncherConfig.ServerPort, ct, timeoutMs: 2000).ConfigureAwait(false))
            {
                AppLog.Warn(LaunchStage.Launch, $"서버 응답 없음: {host}:{LauncherConfig.ServerPort}");
                progress.Report(StageUpdate.Of(LaunchStage.Launch,
                    "서버가 응답하지 않아요(꺼져 있거나 점검 중일 수 있어요). 그래도 게임을 실행할게요."));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { /* ping 실패는 무시하고 진행 */ }
        return host;
    }
}

// (1) Velopack 자체 업데이트. Program.Main 첫 줄의 VelopackApp.Build().Run() 과 짝.
public sealed class VelopackUpdateService : IUpdateService
{
    public async Task<bool> CheckAndApplyAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 확인 중…"));

        // P1-3: 체크/다운로드/적용 실패를 구분해 각각 로그 + graceful 진행(broken 상태 진입 방지).
        UpdateManager mgr;
        UpdateInfo? info;
        try
        {
            var source = new GithubSource(LauncherConfig.UpdateRepoUrl, null, false, null);
            mgr = new UpdateManager(source, null, null);
            if (!mgr.IsInstalled)
            {
                AppLog.Info(LaunchStage.Update, "개발/포터블 빌드(미설치) — 업데이트 생략");
                progress.Report(StageUpdate.Of(LaunchStage.Update, "개발 빌드 — 업데이트 확인 생략", 1.0));
                return false;
            }
            info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            // 체크 실패(소스 부재/네트워크) → graceful skip. 플레이는 계속 가능.
            AppLog.Warn(LaunchStage.Update, "업데이트 확인 실패(계속 진행): " + ex);
            progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 확인 건너뜀 (네트워크를 확인해 주세요)", 1.0));
            return false;
        }

        if (info is null)
        {
            progress.Report(StageUpdate.Of(LaunchStage.Update, "최신 버전입니다", 1.0));
            return false;
        }

        // 다운로드 실패 → 이번엔 건너뛰고 현재 버전으로 진행(다음 실행에 재시도).
        try
        {
            await mgr.DownloadUpdatesAsync(info,
                p => progress.Report(StageUpdate.Of(LaunchStage.Update, $"업데이트 받는 중 {p}%", p / 100.0)),
                ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Update, "업데이트 다운로드 실패(이번엔 건너뜀): " + ex);
            progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 다운로드 실패 — 일단 현재 버전으로 실행할게요.", 1.0));
            return false;
        }

        // 적용 + 재시작 실패 → broken 진입 방지: 로그 + 현재 버전으로 계속.
        try
        {
            AppLog.Info(LaunchStage.Update, "업데이트 적용 및 재시작");
            mgr.ApplyUpdatesAndRestart(info.TargetFullRelease, null);
            return true; // 재시작 예정
        }
        catch (Exception ex)
        {
            AppLog.Error(LaunchStage.Update, "업데이트 적용 실패(현재 버전으로 계속)", ex);
            progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 적용에 실패했어요 — 현재 버전으로 실행할게요.", 1.0));
            return false;
        }
    }
}
