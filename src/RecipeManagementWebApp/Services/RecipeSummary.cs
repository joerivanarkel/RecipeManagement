namespace RecipeManagementWebApp.Services;

/// <summary>Lightweight projection for the browse grid (spec 006). Tags are always empty until Story 008 adds tagging.</summary>
public record RecipeSummary(Guid Id, string Title, string? PhotoUrl, IReadOnlyList<string> Tags);
