using System;

namespace HermaLauncher;

// 런처 고정 설정. 기획서/모드구성 확정값과 정렬.
// ※ Azure client ID 는 비밀이 아니므로 커밋 가능하나, MS 승인(§기획서 R4)이 끝난
//    실제 값으로 교체해야 로그인이 동작한다(미승인/placeholder 시 HTTP 403).
public static class LauncherConfig
{
    // 마인크래프트 / 로더 (모드구성.md 확정)
    public const string MinecraftVersion = "26.1.2";
    public const string FabricLoaderVersion = "";   // "" = Fabric Meta 최신 stable 자동 해석

    // packwiz 팩 URL — 정확히 pack.toml 로 끝나는 전체 URL (구현계획 Codex M3).
    // GitHub Pages(modpack-pages.yml 이 modpack/ 를 배포) 기준. Pages 활성화 필요(공개 레포 또는 유료).
    public const string PackTomlUrl = "https://hermaphrodite-world.github.io/minecraft/pack.toml";

    // 서버 자동 접속 (모드구성: ServerIp 1차 경로).
    // 재빌드 없이 환경변수로 덮어쓰기 가능: HERMA_SERVER_IP (로컬 테스트=127.0.0.1).
    public static readonly string ServerIp =
        Environment.GetEnvironmentVariable("HERMA_SERVER_IP") ?? "play.example.com";
    public static readonly int ServerPort =
        int.TryParse(Environment.GetEnvironmentVariable("HERMA_SERVER_PORT"), out var p) ? p : 25565;

    // 멀티플레이 서버 목록(servers.dat)에 표시될 이름.
    public const string ServerListName = "Hermaphrodite World";

    // Azure 앱 client ID (public client, online 모드 device-code). 환경변수 HERMA_AZURE_CLIENT_ID 로 덮어쓰기 가능.
    // 공개 소스엔 all-zero placeholder(Guid.Empty) 유지 — release 빌드 시 CI 가 secret HERMA_AZURE_CLIENT_ID 로
    // 이 리터럴 1곳을 bake(치환)한다(launcher-build.yml). ※ 게이트는 아래 Guid.Empty 비교라 bake 와 충돌하지 않음.
    public static readonly string AzureClientId =
        Environment.GetEnvironmentVariable("HERMA_AZURE_CLIENT_ID") ?? "00000000-0000-0000-0000-000000000000";

    // 기본 RAM (MB). 추후 호스트 RAM 감지로 동적화(구현계획 M3-3).
    public const int DefaultMaxRamMb = 4096;

    // macOS Dock 표시명 (CmlLib gotcha: 미설정 시 창 포커스 불가)
    public const string MacDockName = "Herma Launcher";

    // 기본 적용 쉐이더(Iris). packwiz 가 shaderpacks/ 에 받은 zip 중 이 prefix 로 시작하는 것을
    // 첫 설치 시 자동 활성화(config/iris.properties). 일치 없으면 첫 zip. 모드구성: Complementary Reimagined.
    public const string DefaultShaderPackPrefix = "ComplementaryReimagined";

    // Velopack 자체 업데이트 소스 (GitHub Releases)
    public const string UpdateRepoUrl = "https://github.com/Hermaphrodite-world/minecraft";

    // 푸터 외부 링크 (스텁 — 실제 URL 확정 시 교체). 빈 값이면 해당 버튼은 동작 안 함(no-op).
    public const string DiscordUrl = "";
    public const string GuideUrl = "";
    public const string WebsiteUrl = "";

    // 오프라인 모드 — LAN/개발 테스트용(online-mode=false 서버/싱글). 기본 false.
    // static readonly (const 아님) — 분기를 compile-time 상수화하지 않아 unreachable 경고 방지.
    public static readonly bool OfflineMode = false;
    public const string OfflineUsername = "Player";

    // 설정 여부 게이트 — placeholder 리터럴을 직접 비교하지 않고 Guid.Empty(=all-zero) 로 판정.
    // 이유: CI bake 가 위 default 리터럴을 실제 ID 로 치환할 때 이 비교식까지 같이 바뀌면
    // realId != realId → 항상 false 가 되어 "승인해도 온라인 로그인이 안 켜지는" silent 버그가 난다.
    // Guid.Empty 비교는 bake 대상 리터럴을 참조하지 않아 안전(placeholder→Empty→false, 실제 ID→true).
    public static bool IsAzureClientConfigured =>
        Guid.TryParse(AzureClientId, out var g) && g != Guid.Empty;
}
