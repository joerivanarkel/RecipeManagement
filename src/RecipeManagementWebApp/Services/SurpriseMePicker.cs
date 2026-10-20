namespace RecipeManagementWebApp.Services;

public record SurprisePick(RecipeAvailability Recipe, bool UsedFallback);

/// <summary>Random, availability-weighted pick that skips recently-cooked recipes (spec 016). Pure/stateless so it's
/// easy to test: callers inject "now" and the random source rather than this reaching for ambient state.</summary>
public static class SurpriseMePicker
{
    private static readonly TimeSpan RecencyWindow = TimeSpan.FromDays(7);

    /// <param name="randomSource">Returns a value in [0, 1). Caller supplies this (e.g. Random.Shared.NextDouble) so tests can be deterministic.</param>
    public static SurprisePick? Pick(IReadOnlyList<RecipeAvailability> candidates, DateTime nowUtc, Func<double> randomSource)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var notRecentlyCooked = candidates
            .Where(c => c.LastCookedAt is null || nowUtc - c.LastCookedAt.Value >= RecencyWindow)
            .ToList();

        var usedFallback = notRecentlyCooked.Count == 0;
        var pool = usedFallback ? candidates : notRecentlyCooked;

        if (pool.Count == 1)
        {
            return new SurprisePick(pool[0], usedFallback);
        }

        // Weighted toward higher availability; a small baseline keeps 0%-available recipes selectable (FR-004, NFR-002).
        var weights = pool.Select(r => r.PercentageAvailable + 0.1).ToList();
        var total = weights.Sum();
        var roll = randomSource() * total;

        var cumulative = 0.0;
        for (var i = 0; i < pool.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                return new SurprisePick(pool[i], usedFallback);
            }
        }

        return new SurprisePick(pool[^1], usedFallback);
    }
}
