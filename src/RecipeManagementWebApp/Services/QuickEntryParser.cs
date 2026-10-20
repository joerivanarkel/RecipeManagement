namespace RecipeManagementWebApp.Services;

/// <summary>Parses the free-text quick-entry box (spec 013) into ingredient names; never persisted.</summary>
public static class QuickEntryParser
{
    private static readonly char[] Separators = [',', '\n', '\r'];

    public static IReadOnlyList<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();
    }
}
