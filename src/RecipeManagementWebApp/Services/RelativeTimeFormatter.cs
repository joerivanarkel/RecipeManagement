namespace RecipeManagementWebApp.Services;

/// <summary>Single shared relative-time formatting rule for "last cooked" displays (spec 017 FR-005),
/// so the grid and detail views never drift into inconsistent phrasing.</summary>
public static class RelativeTimeFormatter
{
    public static string FormatLastCooked(DateTime? lastCookedAtUtc)
    {
        if (lastCookedAtUtc is null)
        {
            return "Never cooked";
        }

        var local = lastCookedAtUtc.Value.ToLocalTime();
        var days = (DateTime.Now.Date - local.Date).Days;

        return days switch
        {
            <= 0 => "today",
            1 => "yesterday",
            < 7 => $"{days} days ago",
            < 30 => Pluralize(days / 7, "week"),
            < 365 => Pluralize(days / 30, "month"),
            _ => Pluralize(days / 365, "year"),
        };
    }

    private static string Pluralize(int count, string unit) =>
        count <= 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
}
