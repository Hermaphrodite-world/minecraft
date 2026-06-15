using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// XSTS XErr → 한국어 안내 매핑 + 코드 추출(기획서 §4.3 로그인 오류 분기).
public class XboxLoginErrorTests
{
    [Theory]
    [InlineData("2148916233", "Xbox 프로필")]
    [InlineData("2148916235", "지역")]
    [InlineData("2148916238", "18세")]
    [InlineData("2148916227", "정지")]
    [InlineData("2148916236", "성인")]
    [InlineData("2148916237", "성인")]
    [InlineData("2148916229", "보호자")]
    public void Known_xerr_maps_to_korean(string xerr, string keyword)
        => Assert.Contains(keyword, XboxLoginError.MessageForXErr(xerr)!);

    [Fact]
    public void Unknown_xerr_is_null()
        => Assert.Null(XboxLoginError.MessageForXErr("2148916999"));

    [Fact]
    public void Null_xerr_is_null()
        => Assert.Null(XboxLoginError.MessageForXErr(null));

    [Theory]
    [InlineData("XSTS error 2148916238 occurred", "2148916238")]
    [InlineData("{\"XErr\":2148916233,\"Message\":\"\"}", "2148916233")]
    [InlineData("no error code here", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void FindXErr_extracts_code(string? text, string? expected)
        => Assert.Equal(expected, XboxLoginError.FindXErr(text));
}
