using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 진행 표시 단계 매핑 — emit 되는 단계가 1..Total 로 단조 증가, 비표시 단계는 null.
public class LaunchStepsTests
{
    [Theory]
    [InlineData(LaunchStage.Update, 1)]
    [InlineData(LaunchStage.Auth, 2)]
    [InlineData(LaunchStage.Java, 3)]
    [InlineData(LaunchStage.Fabric, 3)]
    [InlineData(LaunchStage.Packwiz, 4)]
    [InlineData(LaunchStage.SessionRefresh, 5)]
    [InlineData(LaunchStage.Launch, 5)]
    public void Step_numbers_are_monotonic(LaunchStage stage, int expected)
        => Assert.Equal(expected, LaunchSteps.StepOf(stage));

    [Theory]
    [InlineData(LaunchStage.Idle)]
    [InlineData(LaunchStage.Running)]
    [InlineData(LaunchStage.Failed)]
    public void Non_progress_stages_are_null(LaunchStage stage)
        => Assert.Null(LaunchSteps.StepOf(stage));

    [Fact]
    public void Total_is_5_and_max_step_does_not_exceed_total()
    {
        Assert.Equal(5, LaunchSteps.Total);
        foreach (LaunchStage s in System.Enum.GetValues<LaunchStage>())
        {
            var step = LaunchSteps.StepOf(s);
            if (step is int v) Assert.InRange(v, 1, LaunchSteps.Total);
        }
    }
}
