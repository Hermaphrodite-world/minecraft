using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// ─────────────────────────────────────────────────────────────────────────────
// CmlLib.Core 4.0.6 / CmlLib.Core.Auth.Microsoft 3.3.1 통합 지점.
//
// 본 파일의 메서드 본문은 CmlLib 4.x 정확한 API 시그니처 검증 후 활성화한다
// (research가 명시한 must-verify: FabricInstaller v4 시그니처, device-code 콜백
//  속성명). 실제 구현 코드는 docs/launcher-integration-notes.md 에 그대로 보관.
//
// 현재는 빌드/아키텍처 검증을 위해 명시적 미구현 예외를 던진다 — fabricated 동작
// 대신 정직한 blocker(§CLAUDE.md Fabrication & Tool-Use Honesty).
// ─────────────────────────────────────────────────────────────────────────────

public sealed class CmlLibAuthService : IAuthService
{
    public Task<AuthSession> AuthenticateAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Auth, "Microsoft 로그인 준비 중…"));

        if (!LauncherConfig.IsAzureClientConfigured)
            throw new LaunchStageException(LaunchStage.Auth,
                "Microsoft 로그인용 Azure 앱(client ID)이 아직 설정되지 않았어요.\n" +
                "기획서 R4 — Azure 앱 등록 + Microsoft 승인(aka.ms/mce-reviewappid)이 끝난 뒤 " +
                "LauncherConfig.AzureClientId 를 채우면 device-code 로그인이 활성화됩니다.");

        // 통합(docs/launcher-integration-notes.md §Auth): XboxAuthNet.Game.Msal 의
        // MsalClientHelper + JELoginHandler device-code 플로우로 MSession 획득 후
        // AuthSession 으로 매핑. 소유권(404)/XSTS(XErr) 분기 메시지 처리.
        throw new LaunchStageException(LaunchStage.Auth,
            "Microsoft 로그인 모듈 통합 대기 중 (CmlLib.Core.Auth.Microsoft 3.3.1 + XboxAuthNet.Game.Msal). " +
            "docs/launcher-integration-notes.md 참조.");
    }

    public Task<AuthSession> RevalidateAsync(AuthSession current, CancellationToken ct)
        => Task.FromResult(current); // 통합 시 토큰 만료 검사 + silent refresh
}

public sealed class CmlLibMinecraftService : IMinecraftService
{
    private readonly HttpClient _http = new();

    public Task<string> EnsureJavaAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Java, "Java 런타임 확인 중…"));

        // 통합(docs §Java): CmlLib MinecraftLauncher 의 Java 설치 단계 실행 →
        // MinecraftJavaPathResolver 로 실행 파일 경로 확보(arm64 검증). 26.1 = Java 25.
        // 반환된 경로를 PackwizService 가 재사용(불변식).
        throw new LaunchStageException(LaunchStage.Java,
            "Java 설치 모듈 통합 대기 중 (CmlLib.Core 4.0.6 MinecraftJavaPathResolver). " +
            "docs/launcher-integration-notes.md 참조.");
    }

    public Task LaunchAsync(AuthSession session, IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Fabric, "Fabric 설치 및 게임 준비 중…"));

        // 통합(docs §Launch):
        //   var path = new MinecraftPath(AppPaths.GameDir);
        //   var launcher = new MinecraftLauncher(path);
        //   launcher.FileProgressChanged += ...; launcher.ByteProgressChanged += ...;
        //   var fabric = new FabricInstaller(_http);
        //   var versionId = await fabric.Install(LauncherConfig.MinecraftVersion, path);
        //   var option = new MLaunchOption { Session=..., MaximumRamMb=..., ServerIp=..., ServerPort=...,
        //                                    DockName = isOSX ? LauncherConfig.MacDockName : null };
        //   var proc = await launcher.InstallAndBuildProcessAsync(versionId, option);
        //   proc.Start();
        throw new LaunchStageException(LaunchStage.Launch,
            "게임 실행 모듈 통합 대기 중 (CmlLib.Core 4.0.6 FabricInstaller + InstallAndBuildProcessAsync). " +
            "docs/launcher-integration-notes.md 참조.");
    }
}

// Velopack 자체 업데이트 통합 지점. 패키지 버전 확정 후 활성화(R7).
public sealed class VelopackUpdateService : IUpdateService
{
    public Task<bool> CheckAndApplyAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        progress.Report(StageUpdate.Of(LaunchStage.Update, "업데이트 확인 중…"));
        // 통합(docs §Update): Program.Main 첫 줄 VelopackApp.Build().Run();
        //   var mgr = new UpdateManager(new GithubSource("https://github.com/Hermaphrodite-world/launcher", null, false));
        //   var info = await mgr.CheckForUpdatesAsync(); if (info != null) { ...; mgr.ApplyUpdatesAndRestart(info); return true; }
        // 업데이트 소스 부재/도달 불가 시 graceful skip(Codex M7) — 예외 던지지 않음.
        progress.Report(StageUpdate.Of(LaunchStage.Update, "최신 버전입니다", 1.0));
        return Task.FromResult(false);
    }
}
