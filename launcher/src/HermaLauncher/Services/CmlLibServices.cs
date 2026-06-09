using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
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

        // ── 온라인: 시스템 브라우저 로그인 (요즘 공식 런처와 동일 방식, 크로스플랫폼) ──
        //   클릭 → 기본 브라우저에서 MS 로그인 → loopback 으로 자동 복귀. WebView 불필요.
        if (!LauncherConfig.IsAzureClientConfigured)
            throw new LaunchStageException(LaunchStage.Auth,
                "온라인 로그인은 Azure 앱 client ID 설정이 필요해요(LauncherConfig.AzureClientId 또는 HERMA_AZURE_CLIENT_ID).\n" +
                "지금 테스트하려면 '오프라인 모드'를 켜고 서버를 online-mode=false 로 두세요.");

        progress.Report(StageUpdate.Of(LaunchStage.Auth,
            "기본 브라우저에서 Microsoft 로그인을 진행해 주세요. 완료되면 자동으로 이어집니다."));
        try
        {
            var app = Microsoft.Identity.Client.PublicClientApplicationBuilder
                .Create(LauncherConfig.AzureClientId)
                .WithAuthority("https://login.microsoftonline.com/consumers")
                .WithRedirectUri("http://localhost") // 시스템 브라우저 loopback (random port)
                .Build();

            // 계정 매니저(생성 시 파일에서 자동 로드) → 계정의 SessionStorage 로 AuthenticateContext 구성
            // (없으면 "Context not set" 에러). AuthenticateContext 는 생성자로만 구성(속성 read-only).
            var accountManager = new JsonXboxGameAccountManager(AppPaths.AccountsJson);
            var account = accountManager.GetDefaultAccount() ?? accountManager.NewAccount();

            var authenticator = new NestedAuthenticator
            {
                Context = new AuthenticateContext(account.SessionStorage, _authHttp, ct,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance),
            };
            authenticator.AddMsalOAuth(app, msal => msal.Interactive()); // 시스템 브라우저
            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
            authenticator.AddJEAuthenticator();

            MSession session = await authenticator.ExecuteForLauncherAsync().ConfigureAwait(false);
            try { accountManager.SaveAccounts(); } catch { /* 캐시 저장 실패 무시 */ }
            return new AuthSession(session.Username ?? string.Empty, session.UUID ?? string.Empty,
                                   session.AccessToken ?? string.Empty, IsOffline: false,
                                   Xuid: session.Xuid ?? string.Empty);
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
            // AggregateException 등 내부 메시지를 펼쳐서 표시(이전 '알 수 없는 오류' 개선).
            var detail = ex is AggregateException agg
                ? string.Join(" / ", agg.Flatten().InnerExceptions.Select(e => e.Message))
                : ex.Message;
            throw new LaunchStageException(LaunchStage.Auth, "로그인에 실패했어요.\n" + detail, ex);
        }
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
        progress.Report(StageUpdate.Of(LaunchStage.Java, "Fabric 로더 설치 중…"));
        var fabric = new FabricInstaller(_http);
        _fabricVersionId = await fabric.Install(LauncherConfig.MinecraftVersion, _launcher.MinecraftPath)
                                       .ConfigureAwait(false);

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

    public async Task LaunchAsync(AuthSession session, IProgress<StageUpdate> progress, CancellationToken ct)
    {
        if (_fabricVersionId is null)
            throw new LaunchStageException(LaunchStage.Fabric, "설치 단계가 완료되지 않았어요.");

        progress.Report(StageUpdate.Of(LaunchStage.Launch, "게임 실행 준비 중…"));

        var isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var option = new MLaunchOption
        {
            Session = ToMSession(session),
            MaximumRamMb = LauncherConfig.DefaultMaxRamMb,
            DockName = isOSX ? LauncherConfig.MacDockName : null, // macOS 창 포커스 필수
            // ★ MC 26.1 은 구형 --server/--port 인자를 제거함 → 모던 quickPlayMultiplayer 로 1-클릭 자동 접속.
            ExtraGameArguments = new[]
            {
                new MArgument("--quickPlayMultiplayer"),
                new MArgument($"{LauncherConfig.ServerIp}:{LauncherConfig.ServerPort}"),
            },
        };

        // 이미 EnsureJavaAsync 에서 설치 완료 → build only.
        var proc = await _launcher.BuildProcessAsync(_fabricVersionId, option, ct).ConfigureAwait(false);
        try
        {
            if (!proc.Start())
                throw new LaunchStageException(LaunchStage.Launch, "게임 프로세스를 시작하지 못했어요.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ObjectDisposedException)
        {
            throw new LaunchStageException(LaunchStage.Launch, "게임 실행에 실패했어요. 다시 시도해 주세요.", ex);
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
}

// (1) Velopack 자체 업데이트. Program.Main 첫 줄의 VelopackApp.Build().Run() 과 짝.
public sealed class VelopackUpdateService : IUpdateService
{
    public async Task<bool> CheckAndApplyAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 확인 중…"));
        try
        {
            var source = new GithubSource(LauncherConfig.UpdateRepoUrl, null, false, null);
            var mgr = new UpdateManager(source, null, null);

            if (!mgr.IsInstalled)
            {
                // 개발/포터블 빌드(미설치) — 업데이트 스킵.
                progress.Report(StageUpdate.Of(LaunchStage.Update, "개발 빌드 — 업데이트 확인 생략", 1.0));
                return false;
            }

            var info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info is null)
            {
                progress.Report(StageUpdate.Of(LaunchStage.Update, "최신 버전입니다", 1.0));
                return false;
            }

            await mgr.DownloadUpdatesAsync(info,
                p => progress.Report(StageUpdate.Of(LaunchStage.Update, $"업데이트 받는 중 {p}%", p / 100.0)),
                ct).ConfigureAwait(false);

            mgr.ApplyUpdatesAndRestart(info.TargetFullRelease, null);
            return true; // 재시작 예정
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // 사용자 취소는 graceful-skip 으로 삼키지 않음(Codex) — 실행으로 진행 금지.
        }
        catch (Exception ex)
        {
            // 소스 부재/네트워크 오류 → graceful skip(Codex M7). 예외 전파 금지.
            progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 확인 건너뜀 (" + ex.GetType().Name + ")", 1.0));
            return false;
        }
    }
}
