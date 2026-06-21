using System.IO;
using System;
using HermaLauncher;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 채널 해석 계약(prod/beta/rpg) — 채널별 로더·MC버전·자동접속 불변식 + 레거시 BetaMode 마이그레이션.
//   비-정식(beta/rpg)은 멀티 서버 자동접속을 생략(싱글플레이 테스트)해야 한다.
public class ChannelResolutionTests
{
    [Fact]
    public void Prod_channel_is_fabric_2612_with_autoconnect()
    {
        var ch = LauncherConfig.GetChannel("prod");
        Assert.Equal(LauncherConfig.Loader.Fabric, ch.Loader);
        Assert.Equal(LauncherConfig.MinecraftVersion, ch.MinecraftVersion);
        Assert.Equal(LauncherConfig.PackTomlUrl, ch.PackTomlUrl);
        Assert.True(ch.AutoConnect);
    }

    [Fact]
    public void Beta_channel_is_fabric_without_autoconnect()
    {
        var ch = LauncherConfig.GetChannel("beta");
        Assert.Equal(LauncherConfig.Loader.Fabric, ch.Loader);
        Assert.Equal(LauncherConfig.MinecraftVersion, ch.MinecraftVersion);
        Assert.Equal(LauncherConfig.BetaPackTomlUrl, ch.PackTomlUrl);
        Assert.False(ch.AutoConnect); // 서버 상태 미동기화 — 싱글플레이 테스트
    }

    [Fact]
    public void Rpg_channel_is_neoforge_1211_without_autoconnect()
    {
        var ch = LauncherConfig.GetChannel("rpg");
        Assert.Equal(LauncherConfig.Loader.NeoForge, ch.Loader);
        Assert.Equal(LauncherConfig.RpgMinecraftVersion, ch.MinecraftVersion);
        Assert.Equal("1.21.1", ch.MinecraftVersion);
        Assert.Equal(LauncherConfig.NeoForgeVersion, ch.LoaderVersion);
        Assert.Equal(LauncherConfig.RpgPackTomlUrl, ch.PackTomlUrl);
        Assert.False(ch.AutoConnect);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("unknown")]
    public void Unknown_or_empty_channel_falls_back_to_prod(string? channel)
    {
        var ch = LauncherConfig.GetChannel(channel);
        Assert.Equal(LauncherConfig.Loader.Fabric, ch.Loader);
        Assert.Equal(LauncherConfig.PackTomlUrl, ch.PackTomlUrl);
        Assert.True(ch.AutoConnect);
    }

    [Fact]
    public void EffectiveChannel_prefers_channel_over_legacy_betamode()
    {
        // Channel 이 명시되면 레거시 BetaMode 와 무관하게 Channel 우선.
        var s = new LauncherSettings { Channel = "rpg", BetaMode = true };
        Assert.Equal("rpg", s.EffectiveChannel);
    }

    [Fact]
    public void EffectiveChannel_migrates_legacy_betamode_true_to_beta()
    {
        // 구버전 설정 파일(Channel 키 부재, BetaMode=true) → beta 로 마이그레이션.
        var s = new LauncherSettings { Channel = "", BetaMode = true };
        Assert.Equal("beta", s.EffectiveChannel);
    }

    [Fact]
    public void EffectiveChannel_defaults_to_prod_when_unset()
    {
        Assert.Equal("prod", new LauncherSettings().EffectiveChannel);
    }

    [Fact]
    public void Channel_roundtrips_via_path()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"herma-settings-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(new LauncherSettings { Channel = "rpg" }.Save(tmp));
            Assert.Equal("rpg", LauncherSettings.Load(tmp).Channel);
            Assert.Equal("rpg", LauncherSettings.Load(tmp).EffectiveChannel);
        }
        finally { try { File.Delete(tmp); } catch { /* best-effort */ } }
    }
}
