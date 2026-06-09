using System;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// 인증 세션(런처 내부 표현). Xuid 는 online-mode 서버 접속에 필요(MSession 라운드트립 손실 방지).
public sealed record AuthSession(string Username, string Uuid, string AccessToken, bool IsOffline, string Xuid = "");

public interface IUpdateService
{
    // (1) 자체 업데이트. true = 업데이트 적용 위해 재시작 예정.
    Task<bool> CheckAndApplyAsync(IProgress<StageUpdate> progress, CancellationToken ct);
}

public interface IAuthService
{
    // (2) silent -> device-code fallback + 소유권 검증.
    Task<AuthSession> AuthenticateAsync(IProgress<StageUpdate> progress, CancellationToken ct);

    // (5.5) proc.Start 직전 세션 재검증/refresh.
    Task<AuthSession> RevalidateAsync(AuthSession current, CancellationToken ct);
}

public interface IMinecraftService
{
    // (3) Java 설치/검증 후 실행 파일 경로 반환. packwiz 가 이 경로를 재사용.
    Task<string> EnsureJavaAsync(IProgress<StageUpdate> progress, CancellationToken ct);

    // (5)+(6) Fabric 설치 + 게임 install(해시 검증) + ServerIp 주입 실행.
    Task LaunchAsync(AuthSession session, IProgress<StageUpdate> progress, CancellationToken ct);
}
