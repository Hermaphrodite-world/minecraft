using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 플레이타임 누적 정규화(시계 역행/이상치 방어) + 요약 포맷.
public class PlaytimeTrackerTests
{
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(-5.0, 0)]          // 시계 역행
    [InlineData(90000.0, 0)]       // >24h 이상치(멈춤/슬립 등)
    [InlineData(125.0, 125)]
    [InlineData(86400.0, 86400)]   // 정확히 24h 경계는 허용
    public void DeltaSeconds_clamps_unreal_values(double ran, long expected)
        => Assert.Equal(expected, PlaytimeTracker.DeltaSeconds(ran));

    [Theory]
    [InlineData(0, "기록")]
    [InlineData(1800, "30분")]
    [InlineData(3600, "1시간 0분")]
    [InlineData(8130, "2시간 15분")]
    public void FormatTotal_human_readable(long total, string contains)
        => Assert.Contains(contains, PlaytimeTracker.FormatTotal(total));
}
