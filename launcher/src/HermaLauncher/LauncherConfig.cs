using System;

namespace HermaLauncher;

// 런처 고정 설정. 기획서/모드구성 확정값과 정렬.
// ※ Azure client ID 는 비밀이 아니므로 커밋 가능하나, MS 승인(§기획서 R4)이 끝난
//    실제 값으로 교체해야 로그인이 동작한다(미승인/placeholder 시 HTTP 403).
public static class LauncherConfig
{
    // 마인크래프트 / 로더 (모드구성.md 확정)
    public const string MinecraftVersion = "26.1.2";
    // 테스트 완료한 Fabric loader 버전 핀(P2-2 — 무핀 rolling 방지, pack.toml fabric 과 정합).
    // "" 로 두면 Fabric Meta 최신 stable 자동 해석(비권장 — 배포 후 의도치 않은 변경 가능).
    public const string FabricLoaderVersion = "0.19.3";

    // packwiz 팩 URL — 정확히 pack.toml 로 끝나는 전체 URL (구현계획 Codex M3).
    // GitHub Pages(modpack-pages.yml 이 modpack/ 를 배포) 기준. Pages 활성화 필요(공개 레포 또는 유료).
    // 재빌드 없이 환경변수로 덮어쓰기 가능: HERMA_PACK_URL (로컬 미머지 팩 테스트=http://localhost:8088/pack.toml).
    public static readonly string PackTomlUrl =
        Environment.GetEnvironmentVariable("HERMA_PACK_URL") ?? "https://hermaphrodite-world.github.io/minecraft/pack.toml";

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

    // RAM 미감지/예외 시 폴백 기본값 (MB). 정상 경로는 RamAdvisor 가 호스트 RAM 으로 동적 산정(P3-3).
    public const int DefaultMaxRamMb = 4096;

    // ※ MacDockName 제거(P4): MLaunchOption.DockName 에 공백 포함 값을 주면 macOS 에서 게임이
    //   즉시 종료돼(ClassNotFoundException) DockName 을 설정하지 않기로 함 → 상수도 미사용 dead 라 제거.
    //   사유 상세는 CmlLibServices.LaunchAsync 의 DockName 주석 참조.

    // 기본 적용 쉐이더(Iris). packwiz 가 shaderpacks/ 에 받은 zip 중 이 prefix 로 시작하는 것을
    // 첫 설치 시 자동 활성화(config/iris.properties). 일치 없으면 첫 zip. 모드구성: Complementary Reimagined.
    public const string DefaultShaderPackPrefix = "ComplementaryReimagined";

    // 한국어 번역 보충팩 파일명 토큰. 리소스팩 목록에서 항상 '맨 아래'(options.txt 첫 file 엔트리 =
    // lowest priority = 게임 내 '선택됨' 맨 아래)로 고정하는 식별자 — 다른 팩 lang 을 덮지 않는 fallback.
    public const string TranslationPackToken = "herma-korean";

    // Velopack 자체 업데이트 소스 (GitHub Releases)
    public const string UpdateRepoUrl = "https://github.com/Hermaphrodite-world/minecraft";

    // 운영자 공지/점검 원격 소스(news.json). 환경변수 HERMA_NEWS_URL 로 주입. 빈 값이면 공지 기능 off(graceful).
    // 예: GitHub Pages 의 https://hermaphrodite-world.github.io/minecraft/news.json
    public static readonly string NewsUrl =
        Environment.GetEnvironmentVariable("HERMA_NEWS_URL") ?? "";

    // 푸터 외부 링크. 재빌드 없이 환경변수로 주입 가능(HERMA_DISCORD_URL / HERMA_GUIDE_URL /
    // HERMA_WEBSITE_URL). 빈 값이면 해당 버튼을 UI 에서 숨긴다(P3-6 — no-op 버튼 노출 방지).
    public static readonly string DiscordUrl =
        Environment.GetEnvironmentVariable("HERMA_DISCORD_URL") ?? "";
    public static readonly string GuideUrl =
        Environment.GetEnvironmentVariable("HERMA_GUIDE_URL") ?? "";
    public static readonly string WebsiteUrl =
        Environment.GetEnvironmentVariable("HERMA_WEBSITE_URL") ?? "";

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
