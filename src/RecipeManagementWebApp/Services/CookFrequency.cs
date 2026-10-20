namespace RecipeManagementWebApp.Services;

public record CookFrequency(
    int TotalCount,
    DateTime? LastCookedAt,
    int FrequencyThisMonth,
    int FrequencyThisWeek);
