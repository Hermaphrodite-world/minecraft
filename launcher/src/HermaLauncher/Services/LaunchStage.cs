using System;

namespace HermaLauncher.Services;

// 구현계획 §4 실행 순서 불변식의 단계.
public enum LaunchStage
{
    Idle,
    Update,        // (1) Velopack 자체 업데이트
    Auth,          // (2) MS device-code (silent -> fallback) + 소유권 검증
    Java,          // (3) CmlLib Java 설치/검증 -> JavaPath 캐싱
    Packwiz,       // (4) packwiz 동기화 (3에서 얻은 java 재사용)
    Fabric,        // (5) Fabric 설치 + 게임 install (해시 검증)
    SessionRefresh,// (5.5) proc.Start 직전 세션 재검증
    Launch,        // (6) 서버 ping -> ServerIp 주입 -> 실행
    Running,
    Failed,
}

// UI로 흘려보내는 진행 상태 스냅샷.
public readonly record struct StageUpdate(
    LaunchStage Stage,
    string Message,
    double? Fraction = null,   // 0..1, null이면 indeterminate
    bool IsError = false)
{
    public static StageUpdate Of(LaunchStage stage, string message, double? fraction = null)
        => new(stage, message, fraction);

    public static StageUpdate Error(LaunchStage stage, string message)
        => new(stage, message, null, true);
}

// 단계 실패를 사용자 친화 메시지로 감싸는 예외(구현계획 Codex H1 — 실패 게이트).
public sealed class LaunchStageException : Exception
{
    public LaunchStage Stage { get; }

    public LaunchStageException(LaunchStage stage, string userMessage, Exception? inner = null)
        : base(userMessage, inner)
    {
        Stage = stage;
    }
}
