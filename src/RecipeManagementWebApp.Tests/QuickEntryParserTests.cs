using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class QuickEntryParserTests
{
    [Fact]
    public void Parse_CommaSeparated_TrimsEachSegment()
    {
        var result = QuickEntryParser.Parse("chicken,  cabbage ,rice");

        Assert.Equal(["chicken", "cabbage", "rice"], result);
    }

    [Fact]
    public void Parse_TrailingComma_IgnoresEmptySegment()
    {
        var result = QuickEntryParser.Parse("chicken, cabbage,");

        Assert.Equal(["chicken", "cabbage"], result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",, ,")]
    public void Parse_EmptyOrWhitespaceOnly_ReturnsEmpty(string? text)
    {
        Assert.Empty(QuickEntryParser.Parse(text));
    }

    [Fact]
    public void Parse_Newlines_AreAlsoTreatedAsSeparators()
    {
        var result = QuickEntryParser.Parse("chicken\ncabbage\r\nrice");

        Assert.Equal(["chicken", "cabbage", "rice"], result);
    }
}
