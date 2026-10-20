using RecipeManagementWebApp.Domain;

namespace RecipeManagementWebApp.Services;

public record CookHistorySummary(
    IReadOnlyList<CookLogEntry> Entries,
    int TotalCount,
    int CountThisMonth,
    int CountThisWeek);
