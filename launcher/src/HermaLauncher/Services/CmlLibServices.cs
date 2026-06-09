using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

namespace HermaLauncher.Services;

// SynchronizationContext 마샬링 없이 무시하는 진행률 싱크(고빈도 byte progress UI flood 방지 — Codex).
internal sealed class NullProgress<T> : IProgress<T>
{
    public static readonly NullProgress<T> Instance = new();
    public void Report(T value) { }
}

// CmlLib.Core 4.0.6 / Auth.Microsoft 3.3.1 / Velopack 1.2.0 실제 통합.
// 모든 API 는 복원된 어셈블리 리플렉션으로 시그니처 검증함(docs/launcher-integration-notes.md).

// (2) 인증 — Windows 는 CmlLib 기본 OAuth(자체 Azure 앱 불필요). macOS device-code 는 최종 단계.
public sealed class CmlLibAuthService : IAuthService
{
    public async Task<AuthSession> AuthenticateAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Auth, "Microsoft 로그인 중…"));

        // 오프라인 모드(LAN/개발): 실제 MS 인증 없이 진행.
        if (LauncherConfig.OfflineMode)
            return new AuthSession(LauncherConfig.OfflineUsername, Guid.Empty.ToString("N"), "0", IsOffline: true);

        try
        {
            // BuildDefault() 는 static — 기본 OAuth/Xbox 프로바이더 + 기본 토큰 캐시(로그인 1회 유지).
            var handler = JELoginHandlerBuilder.BuildDefault();

            // silent(캐시) 우선 → 실패 시 interactive. Windows 기본 OAuth 는 CmlLib client ID 사용.
            MSession session = await handler.Authenticate(ct).ConfigureAwait(false);
            return new AuthSession(session.Username ?? string.Empty, session.UUID ?? string.Empty,
                                   session.AccessToken ?? string.Empty, IsOffline: false,
                                   Xuid: session.Xuid ?? string.Empty);
        }
        catch (JEAuthException ex)
        {
            // 소유권/프로필 오류 분기 (404 = Java Edition 미보유)
            var msg = ex.StatusCode == 404
                ? "이 계정은 Minecraft: Java Edition 을 소유하고 있지 않아요. 구매 또는 계정을 확인해 주세요."
                : $"로그인에 실패했어요: {ex.ErrorMessage ?? ex.Message}";
            throw new LaunchStageException(LaunchStage.Auth, msg, ex);
        }
    }

    public async Task<AuthSession> RevalidateAsync(AuthSession current, CancellationToken ct)
    {
        if (current.IsOffline)
            return current;
        try
        {
            // 설치 동안 만료됐을 수 있는 토큰을 silent refresh.
            var handler = JELoginHandlerBuilder.BuildDefault();
            var session = await handler.AuthenticateSilently(ct).ConfigureAwait(false);

            // 다중 캐시 계정 환경에서 silent 가 '다른 계정'을 반환할 수 있음(Codex) →
            // 원래 로그인한 계정(UUID)과 일치할 때만 갱신 세션 채택, 아니면 기존 유지.
            if (!string.IsNullOrEmpty(session.UUID) &&
                !string.Equals(session.UUID, current.Uuid, StringComparison.OrdinalIgnoreCase))
                return current;

            return new AuthSession(session.Username ?? current.Username, session.UUID ?? current.Uuid,
                                   session.AccessToken ?? current.AccessToken, IsOffline: false,
                                   Xuid: session.Xuid ?? current.Xuid);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return current; // refresh 실패 시 기존 세션으로 시도(실패하면 실행 단계에서 보고)
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
            ServerIp = LauncherConfig.ServerIp,     // 1-클릭 자동 접속
            ServerPort = LauncherConfig.ServerPort,
            DockName = isOSX ? LauncherConfig.MacDockName : null, // macOS 창 포커스 필수
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
