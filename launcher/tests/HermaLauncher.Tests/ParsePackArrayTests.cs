using System.Collections.Generic;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// options.txt 의 resourcePacks 배열 파싱(따옴표 인식). 이번 세션 실제 버그(unquoted
// lambdabettergrass:default 로 배열 깨짐) 회귀 방지.
public class ParsePackArrayTests
{
    [Fact]
    public void Parses_quoted_entries_preserving_quotes()
    {
        var lines = new List<string> { "resourcePacks:[\"vanilla\",\"file/a.zip\"]" };
        var result = ClientDefaults.ParsePackArray(lines, "resourcePacks:");
        Assert.Equal(new[] { "\"vanilla\"", "\"file/a.zip\"" }, result);
    }

    [Fact]
    public void Preserves_unquoted_token_with_colon()
    {
        // lambdabettergrass:default 같은 무따옴표 토큰을 콜론에서 자르지 않고 보존해야 함.
        var lines = new List<string> { "resourcePacks:[\"vanilla\",lambdabettergrass:default,\"file/x.zip\"]" };
        var result = ClientDefaults.ParsePackArray(lines, "resourcePacks:");
        Assert.Equal(3, result.Count);
        Assert.Equal("lambdabettergrass:default", result[1]);
    }

    [Fact]
    public void Does_not_split_comma_inside_quotes()
    {
        var lines = new List<string> { "resourcePacks:[\"a,b\",\"c\"]" };
        var result = ClientDefaults.ParsePackArray(lines, "resourcePacks:");
        Assert.Equal(2, result.Count);
        Assert.Equal("\"a,b\"", result[0]);
    }

    [Fact]
    public void Missing_key_returns_empty()
        => Assert.Empty(ClientDefaults.ParsePackArray(new List<string> { "someOther:true" }, "resourcePacks:"));

    [Fact]
    public void Empty_array_returns_empty()
        => Assert.Empty(ClientDefaults.ParsePackArray(new List<string> { "resourcePacks:[]" }, "resourcePacks:"));
}
