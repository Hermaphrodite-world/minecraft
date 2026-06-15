namespace HermaLauncher.Services;

// 플레이 시간 누적·표시(순수 함수 — 단위 테스트 가능). 시계 역행/이상치 방어 + 사람이 읽는 요약.
public static class PlaytimeTracker
{
    // 한 세션 실행시간(초)을 누적에 더할 값으로 정규화. 0 이하·비현실치(>24h, 시계 역행/멈춤 등)는 0.
    public static long DeltaSeconds(double ranSeconds)
        => ranSeconds > 0 && ranSeconds <= 86400 ? (long)ranSeconds : 0;

    // 누적 초 → "총 N시간 M분" / "총 M분" / 기록 없음.
    public static string FormatTotal(long totalSeconds)
    {
        if (totalSeconds <= 0) return "아직 플레이 기록이 없어요";
        var h = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        return h > 0 ? $"총 {h}시간 {m}분 플레이" : $"총 {m}분 플레이";
    }
}
