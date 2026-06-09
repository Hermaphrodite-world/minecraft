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

    // Azure 앱 client ID (public client, online 모드 device-code). 환경변수 HERMA_AZURE_CLIENT_ID 로 덮어쓰기 가능.
    public static readonly string AzureClientId =
        Environment.GetEnvironmentVariable("HERMA_AZURE_CLIENT_ID") ?? "00000000-0000-0000-0000-000000000000";

    // 기본 RAM (MB). 추후 호스트 RAM 감지로 동적화(구현계획 M3-3).
    public const int DefaultMaxRamMb = 4096;

    // macOS Dock 표시명 (CmlLib gotcha: 미설정 시 창 포커스 불가)
    public const string MacDockName = "Herma Launcher";

    // Velopack 자체 업데이트 소스 (GitHub Releases)
    public const string UpdateRepoUrl = "https://github.com/Hermaphrodite-world/minecraft";

    // 오프라인 모드 — LAN/개발 테스트용(online-mode=false 서버/싱글). 기본 false.
    // static readonly (const 아님) — 분기를 compile-time 상수화하지 않아 unreachable 경고 방지.
    public static readonly bool OfflineMode = false;
    public const string OfflineUsername = "Player";

    public static bool IsAzureClientConfigured =>
        !string.IsNullOrWhiteSpace(AzureClientId) &&
        AzureClientId != "00000000-0000-0000-0000-000000000000";
}
