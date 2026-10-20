using System.Globalization;
using System.Text.RegularExpressions;

namespace RecipeManagementWebApp.Services;

public record DetectedDuration(string RawMatch, int DurationSeconds);

/// <summary>Pattern-based (not NLP) duration detection for cook mode step text (spec 021).</summary>
public static partial class DurationDetector
{
    public static IReadOnlyList<DetectedDuration> Detect(string text)
    {
        var results = new List<DetectedDuration>();
        var consumedRanges = new List<(int Start, int End)>();

        foreach (Match match in HourMinutePattern().Matches(text))
        {
            var hours = double.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
            var minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
            var seconds = (int)(hours * 3600) + (minutes * 60);
            results.Add(new DetectedDuration(match.Value.Trim(), seconds));
            consumedRanges.Add((match.Index, match.Index + match.Length));
        }

        foreach (Match match in SingleUnitPattern().Matches(text))
        {
            if (consumedRanges.Any(r => match.Index >= r.Start && match.Index < r.End))
            {
                continue;
            }

            var value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
            var unit = match.Groups["unit"].Value.ToLowerInvariant();
            var seconds = unit switch
            {
                _ when unit.StartsWith("hour") || unit.StartsWith("hr") => (int)(value * 3600),
                _ when unit.StartsWith("min") => (int)(value * 60),
                _ => (int)value,
            };

            results.Add(new DetectedDuration(match.Value.Trim(), seconds));
        }

        return results;
    }

    [GeneratedRegex(@"(?<h>\d+(?:\.\d+)?)\s*(?:hours?|hrs?)\s+(?<m>\d+)\s*(?:minutes?|mins?)", RegexOptions.IgnoreCase)]
    private static partial Regex HourMinutePattern();

    [GeneratedRegex(@"(?<value>\d+(?:\.\d+)?)\s*(?<unit>hours?|hrs?|minutes?|mins?|seconds?|secs?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SingleUnitPattern();
}
