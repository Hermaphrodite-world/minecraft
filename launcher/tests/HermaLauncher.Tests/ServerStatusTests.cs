using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// MC status JSON 파싱 — players/MOTD(문자열·컴포넌트), 형식 불일치/빈 입력 = null.
public class ServerStatusTests
{
    [Fact]
    public void Parses_players_and_string_motd()
    {
        var st = ServerStatus.Parse("{\"players\":{\"online\":2,\"max\":20},\"description\":\"Welcome!\"}");
        Assert.NotNull(st);
        Assert.Equal(2, st!.Players);
        Assert.Equal(20, st.MaxPlayers);
        Assert.Equal("Welcome!", st.Motd);
    }

    [Fact]
    public void Parses_component_object_motd_with_extra()
    {
        var st = ServerStatus.Parse("{\"players\":{\"online\":0,\"max\":10},\"description\":{\"text\":\"Herma\",\"extra\":[{\"text\":\" World\"}]}}");
        Assert.NotNull(st);
        Assert.Equal(0, st!.Players);
        Assert.Equal("Herma World", st.Motd);
    }

    [Fact]
    public void Missing_players_yields_null_counts_not_failure()
    {
        var st = ServerStatus.Parse("{\"description\":\"x\"}");
        Assert.NotNull(st);
        Assert.Null(st!.Players);
        Assert.Null(st.MaxPlayers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[1,2,3]")]
    public void Invalid_input_is_null(string? json)
        => Assert.Null(ServerStatus.Parse(json));
}
