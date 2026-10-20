using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class RelativeTimeFormatterTests
{
    [Fact]
    public void FormatLastCooked_Null_ReturnsNeverCooked()
    {
        Assert.Equal("Never cooked", RelativeTimeFormatter.FormatLastCooked(null));
    }

    [Fact]
    public void FormatLastCooked_Today_ReturnsToday()
    {
        Assert.Equal("today", RelativeTimeFormatter.FormatLastCooked(DateTime.UtcNow));
    }

    [Fact]
    public void FormatLastCooked_Yesterday_ReturnsYesterday()
    {
        Assert.Equal("yesterday", RelativeTimeFormatter.FormatLastCooked(DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void FormatLastCooked_ThreeDaysAgo_ReturnsDaysAgo()
    {
        Assert.Equal("3 days ago", RelativeTimeFormatter.FormatLastCooked(DateTime.UtcNow.AddDays(-3)));
    }

    [Fact]
    public void FormatLastCooked_TenDaysAgo_ReturnsWeeksAgo()
    {
        Assert.Equal("1 week ago", RelativeTimeFormatter.FormatLastCooked(DateTime.UtcNow.AddDays(-10)));
    }

    [Fact]
    public void FormatLastCooked_EightMonthsAgo_ReturnsMonthsAgo()
    {
        var result = RelativeTimeFormatter.FormatLastCooked(DateTime.UtcNow.AddDays(-240));

        Assert.Equal("8 months ago", result);
    }

    [Fact]
    public void FormatLastCooked_OverAYearAgo_ReturnsYearsAgo()
    {
        Assert.Equal("1 year ago", RelativeTimeFormatter.FormatLastCooked(DateTime.UtcNow.AddDays(-400)));
    }
}
