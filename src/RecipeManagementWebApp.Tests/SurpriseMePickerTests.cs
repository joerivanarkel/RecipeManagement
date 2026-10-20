using RecipeManagementWebApp.Services;
using Xunit;

namespace RecipeManagementWebApp.Tests;

public class SurpriseMePickerTests
{
    private static RecipeAvailability Candidate(string title, int available, int total, DateTime? lastCookedAt = null) =>
        new(Guid.NewGuid(), title, null, available, total, [], [], [], lastCookedAt);

    [Fact]
    public void Pick_EmptyCandidates_ReturnsNull()
    {
        var pick = SurpriseMePicker.Pick([], DateTime.UtcNow, () => 0.5);

        Assert.Null(pick);
    }

    [Fact]
    public void Pick_SingleCandidate_IsDeterministic()
    {
        var only = Candidate("Soup", 2, 2);

        var pick = SurpriseMePicker.Pick([only], DateTime.UtcNow, () => 0.999);

        Assert.Same(only, pick!.Recipe);
        Assert.False(pick.UsedFallback);
    }

    [Fact]
    public void Pick_ExcludesRecentlyCooked()
    {
        var now = DateTime.UtcNow;
        var recent = Candidate("Stir-Fry", 5, 5, now.AddDays(-2));
        var old = Candidate("Pasta", 5, 5, now.AddDays(-30));
        var never = Candidate("Soup", 5, 5, null);

        // Roll near 1.0 would pick the last item in the weighted pool if both remain eligible.
        var pick = SurpriseMePicker.Pick([recent, old, never], now, () => 0.999);

        Assert.NotEqual("Stir-Fry", pick!.Recipe.Title);
        Assert.False(pick.UsedFallback);
    }

    [Fact]
    public void Pick_AllRecentlyCooked_FallsBackToFullPoolAndFlagsIt()
    {
        var now = DateTime.UtcNow;
        var a = Candidate("Stir-Fry", 5, 5, now.AddDays(-1));
        var b = Candidate("Soup", 5, 5, now.AddDays(-3));

        var pick = SurpriseMePicker.Pick([a, b], now, () => 0.0);

        Assert.NotNull(pick);
        Assert.True(pick!.UsedFallback);
    }

    [Fact]
    public void Pick_WeightedTowardHigherAvailability_OverManyTrials()
    {
        var now = DateTime.UtcNow;
        var high = Candidate("High", 10, 10); // 100%
        var low = Candidate("Low", 2, 10); // 20%

        var random = new Random(42);
        var highCount = 0;
        const int trials = 2000;

        for (var i = 0; i < trials; i++)
        {
            var pick = SurpriseMePicker.Pick([high, low], now, random.NextDouble);
            if (pick!.Recipe.Title == "High")
            {
                highCount++;
            }
        }

        // 100% should be picked meaningfully more often than 20%, but not deterministically every time.
        Assert.InRange(highCount, trials * 0.55, trials * 0.95);
    }

    [Fact]
    public void Pick_ZeroPercentCandidate_IsStillSelectableGivenEnoughTrials()
    {
        var now = DateTime.UtcNow;
        var zero = Candidate("ZeroAvailable", 0, 10);
        var full = Candidate("FullAvailable", 10, 10);

        var random = new Random(7);
        var zeroWasPicked = false;

        for (var i = 0; i < 500 && !zeroWasPicked; i++)
        {
            var pick = SurpriseMePicker.Pick([zero, full], now, random.NextDouble);
            zeroWasPicked = pick!.Recipe.Title == "ZeroAvailable";
        }

        Assert.True(zeroWasPicked);
    }
}
