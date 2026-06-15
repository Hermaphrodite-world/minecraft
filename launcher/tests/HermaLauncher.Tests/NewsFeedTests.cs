using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 운영자 공지/점검 news.json 파싱 — maintenance, items 필터(필수 누락 건너뜀), urgent, latest, 잘못된 입력.
public class NewsFeedTests
{
    [Fact]
    public void Parses_maintenance_active_with_message()
    {
        var f = NewsFeed.Parse("{\"maintenance\":{\"active\":true,\"message\":\"22시 점검\"}}");
        Assert.NotNull(f);
        Assert.True(f!.Maintenance!.Active);
        Assert.Equal("22시 점검", f.Maintenance.Message);
    }

    [Fact]
    public void Maintenance_inactive_when_active_false_or_absent()
    {
        Assert.False(NewsFeed.Parse("{\"maintenance\":{\"active\":false}}")!.Maintenance!.Active);
        Assert.Null(NewsFeed.Parse("{\"items\":[]}")!.Maintenance);
    }

    [Fact]
    public void Parses_items_and_latest_and_urgent()
    {
        var f = NewsFeed.Parse(
            "{\"items\":[{\"id\":\"a\",\"title\":\"첫 공지\",\"urgent\":true},{\"id\":\"b\",\"title\":\"둘째\",\"body\":\"본문\"}]}");
        Assert.NotNull(f);
        Assert.Equal(2, f!.Items.Count);
        Assert.Equal("첫 공지", f.Latest!.Title);   // 첫 항목이 latest
        Assert.True(f.Latest.Urgent);
        Assert.Equal("본문", f.Items[1].Body);
    }

    [Fact]
    public void Skips_items_missing_required_id_or_title()
    {
        var f = NewsFeed.Parse(
            "{\"items\":[{\"title\":\"id없음\"},{\"id\":\"x\"},{\"id\":\"ok\",\"title\":\"정상\"}]}");
        Assert.NotNull(f);
        Assert.Single(f!.Items);
        Assert.Equal("ok", f.Latest!.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[1,2,3]")]
    public void Invalid_input_is_null(string? json)
        => Assert.Null(NewsFeed.Parse(json));

    [Fact]
    public void Empty_object_yields_empty_feed_not_null()
    {
        var f = NewsFeed.Parse("{}");
        Assert.NotNull(f);
        Assert.Null(f!.Maintenance);
        Assert.Empty(f.Items);
        Assert.Null(f.Latest);
    }
}
