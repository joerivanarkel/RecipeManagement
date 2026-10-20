using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public class CookLogService(RecipeDbContext dbContext) : ICookLogService
{
    public async Task<CookLogEntry?> RecordCookAsync(
        Guid recipeId, int servings, CancellationToken cancellationToken = default)
    {
        var recipeExists = await dbContext.Recipes.AnyAsync(r => r.Id == recipeId, cancellationToken);
        if (!recipeExists)
        {
            return null;
        }

        // Fall back to the recipe's own servings if an invalid value (0 or negative) was passed from cook mode (EC-006).
        var effectiveServings = servings > 0
            ? servings
            : await dbContext.Recipes.Where(r => r.Id == recipeId).Select(r => r.Servings).FirstAsync(cancellationToken);

        var entry = new CookLogEntry
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            CookedAt = DateTime.UtcNow,
            Servings = effectiveServings,
        };

        dbContext.CookLogEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public async Task<CookFrequency> GetFrequencyAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.CookLogEntries
            .Where(c => c.RecipeId == recipeId)
            .Select(c => c.CookedAt)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new CookFrequency(0, null, 0, 0);
        }

        // Boundaries are computed in local calendar time (so "this week"/"this month" match what the
        // user actually experiences), then converted to UTC for comparison against the stored UTC timestamps.
        var nowLocal = DateTime.Now;
        var startOfMonthLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var startOfWeekLocal = nowLocal.Date.AddDays(-(int)nowLocal.DayOfWeek);
        var startOfMonthUtc = startOfMonthLocal.ToUniversalTime();
        var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

        return new CookFrequency(
            TotalCount: entries.Count,
            LastCookedAt: entries.Max(),
            FrequencyThisMonth: entries.Count(e => e >= startOfMonthUtc),
            FrequencyThisWeek: entries.Count(e => e >= startOfWeekUtc));
    }
}
