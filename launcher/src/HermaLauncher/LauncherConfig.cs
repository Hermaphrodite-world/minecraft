namespace HermaLauncher;

// 런처 고정 설정. 기획서/모드구성 확정값과 정렬.
// ※ Azure client ID 는 비밀이 아니므로 커밋 가능하나, MS 승인(§기획서 R4)이 끝난
//    실제 값으로 교체해야 로그인이 동작한다(미승인/placeholder 시 HTTP 403).
public static class LauncherConfig
{
    // 마인크래프트 / 로더 (모드구성.md 확정)
    public const string MinecraftVersion = "26.1.2";
    public const string FabricLoaderVersion = "";   // "" = Fabric Meta 최신 stable 자동 해석

    // packwiz 팩 URL — 정확히 pack.toml 로 끝나는 전체 URL (구현계획 Codex M3)
    // GitHub Pages 호스팅 예시. 실제 배포 URL 로 교체.
    public const string PackTomlUrl = "https://hermaphrodite-world.github.io/modpack/pack.toml";

    // 서버 자동 접속 (모드구성: ServerIp 1차 경로)
    public const string ServerIp = "play.example.com";
    public const int ServerPort = 25565;

    // Azure 앱 client ID (public client). MS 승인 필요. placeholder 면 로그인 비활성.
    public const string AzureClientId = "00000000-0000-0000-0000-000000000000";

    // 기본 RAM (MB). 추후 호스트 RAM 감지로 동적화(구현계획 M3-3).
    public const int DefaultMaxRamMb = 4096;

    // macOS Dock 표시명 (CmlLib gotcha: 미설정 시 창 포커스 불가)
    public const string MacDockName = "Herma Launcher";

    public static bool IsAzureClientConfigured =>
        !string.IsNullOrWhiteSpace(AzureClientId) &&
        AzureClientId != "00000000-0000-0000-0000-000000000000";
}
