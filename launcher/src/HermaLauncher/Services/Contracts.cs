using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// 인증 세션(런처 내부 표현). Xuid 는 online-mode 서버 접속에 필요(MSession 라운드트립 손실 방지).
public sealed record AuthSession(string Username, string Uuid, string AccessToken, bool IsOffline, string Xuid = "");

// Play 시 사용자가 고른 로그인 옵션.
//  Offline=true  : MS 로그인 없이 username 만 (online-mode=false 친구 서버용 — 가장 단순).
//  Offline=false : device-code MS 로그인 (online-mode=true 서버용 — Azure 앱 client ID 필요).
public sealed record LaunchOptions(string Username, bool Offline);

public interface IUpdateService
{
    // (1) 자체 업데이트. true = 업데이트 적용 위해 재시작 예정.
    Task<bool> CheckAndApplyAsync(IProgress<StageUpdate> progress, CancellationToken ct);
}

public interface IAuthService
{
    // (2) offline(username) 또는 online 로그인(silent 우선 → 브라우저 fallback).
    Task<AuthSession> AuthenticateAsync(LaunchOptions options, IProgress<StageUpdate> progress, CancellationToken ct);

    // (5.5) proc.Start 직전 세션 재검증/갱신(긴 설치 중 토큰 만료 대응). best-effort — 실패 시 current 반환.
    Task<AuthSession> RevalidateAsync(AuthSession current, IProgress<StageUpdate> progress, CancellationToken ct);
}

public interface IMinecraftService
{
    // (3) Java 설치/검증 후 실행 파일 경로 반환. packwiz 가 이 경로를 재사용.
    Task<string> EnsureJavaAsync(IProgress<StageUpdate> progress, CancellationToken ct);

    // (5)+(6) Fabric 설치 + 게임 install(해시 검증) + endpoint 주입 실행.
    //   endpoint: quickPlay 자동접속 대상(servers.dat 등록과 동일 주소 — 오케스트레이터가 한 번 해석해 공유).
    //   autoConnect=false(베타 모드): quickPlay 인자를 생략하고 메인 메뉴로 실행(싱글플레이 테스트).
    // 시작된 게임 프로세스를 반환한다(런처가 종료 모니터링 후 자신을 닫기 위함). 호출자가 Dispose 소유.
    Task<Process> LaunchAsync(AuthSession session, ServerEndpoint endpoint, IProgress<StageUpdate> progress, CancellationToken ct, bool autoConnect = true);
}

// (4) packwiz 모드팩 동기화. LaunchOrchestrator 단위 테스트를 위해 인터페이스로 분리(Codex Test-R1).
public interface IPackwizService
{
    Task RunAsync(string javaExecutable, string packTomlUrl, IProgress<StageUpdate> progress,
        CancellationToken ct, string? packFolder = null);
}
