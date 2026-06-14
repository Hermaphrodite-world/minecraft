using System.Text.Json;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// RAM 권장값 경계 + 설정 모델 직렬화(P3-3/P3-2).
public class RamAndSettingsTests
{
    // 저사양 호스트에선 권장값이 일반 하한(2048) 밑(최저 LowHostMinRamMb)으로 내려갈 수 있어 하한을 분리.
    [Fact]
    public void Recommended_ram_within_bounds()
        => Assert.InRange(RamAdvisor.RecommendedMaxRamMb(), RamAdvisor.LowHostMinRamMb, RamAdvisor.MaxRamMb);

    [Fact]
    public void Effective_ram_within_bounds()
        => Assert.InRange(RamAdvisor.EffectiveMaxRamMb(), RamAdvisor.LowHostMinRamMb, RamAdvisor.MaxRamMb);

    [Fact]
    public void Settings_override_roundtrips()
    {
        var json = JsonSerializer.Serialize(new LauncherSettings { MaxRamMbOverride = 6144 });
        var back = JsonSerializer.Deserialize<LauncherSettings>(json)!;
        Assert.Equal(6144, back.MaxRamMbOverride);
        Assert.False(back.IsRamAuto);
    }

    [Fact]
    public void Null_override_is_auto()
        => Assert.True(new LauncherSettings { MaxRamMbOverride = null }.IsRamAuto);

    [Fact]
    public void Nonpositive_override_is_auto()
        => Assert.True(new LauncherSettings { MaxRamMbOverride = 0 }.IsRamAuto);
}
