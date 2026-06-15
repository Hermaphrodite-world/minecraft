using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 접속 host 결정 정책(접속 이슈 대응) — override 최우선 → 로컬 감지 → 공개. + 입력 정규화.
public class ServerHostResolverTests
{
    [Fact]
    public void Override_present_wins_even_when_local_server_up()
        => Assert.Equal(ServerHostResolver.Source.UserOverride,
                        ServerHostResolver.Decide("192.168.0.5", localServerUp: true));

    [Fact]
    public void No_override_local_up_is_local()
        => Assert.Equal(ServerHostResolver.Source.Local, ServerHostResolver.Decide(null, localServerUp: true));

    [Fact]
    public void No_override_local_down_is_public()
        => Assert.Equal(ServerHostResolver.Source.Public, ServerHostResolver.Decide(null, localServerUp: false));

    [Fact]
    public void Whitespace_override_is_ignored()
        => Assert.Equal(ServerHostResolver.Source.Public, ServerHostResolver.Decide("   ", localServerUp: false));

    [Theory]
    [InlineData("  192.168.219.102  ", "192.168.219.102")]
    [InlineData("tcp://192.168.219.102", "192.168.219.102")]
    [InlineData("192.168.219.102/", "192.168.219.102")]
    [InlineData("play.example.com", "play.example.com")]
    public void Normalize_trims_scheme_and_slashes(string raw, string expected)
        => Assert.Equal(expected, ServerHostResolver.Normalize(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_blank_is_null(string? raw)
        => Assert.Null(ServerHostResolver.Normalize(raw));
}
