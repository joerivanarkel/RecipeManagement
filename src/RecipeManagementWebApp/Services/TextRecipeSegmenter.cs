using System.Text.RegularExpressions;

namespace RecipeManagementWebApp.Services;

public record SegmentedRecipe(string Title, IReadOnlyList<DraftIngredientLine> Ingredients, IReadOnlyList<string> Steps);

/// <summary>Best-effort heuristic segmentation of raw recipe text into title/ingredients/steps (spec 003),
/// reused as-is after OCR extraction for photo imports (spec 004 FR-005). Never throws -- worst case is
/// poor segmentation, not failure (spec 003 NFR-002).</summary>
public static partial class TextRecipeSegmenter
{
    public static SegmentedRecipe Segment(string rawText)
    {
        var lines = (rawText ?? string.Empty)
            .Split(['\r', '\n'])
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return new SegmentedRecipe(string.Empty, [], []);
        }

        var title = string.Empty;
        var startIndex = 0;
        var firstLine = StripListMarker(lines[0]);
        if (!LooksLikeIngredient(firstLine) && !LooksLikeStep(firstLine))
        {
            title = firstLine;
            startIndex = 1;
        }

        var ingredients = new List<DraftIngredientLine>();
        var steps = new List<string>();

        for (var i = startIndex; i < lines.Count; i++)
        {
            var line = StripListMarker(lines[i]);
            if (line.Length == 0)
            {
                continue;
            }

            if (LooksLikeStep(line) && !LooksLikeIngredient(line))
            {
                steps.Add(line);
            }
            else
            {
                ingredients.Add(ParseIngredientLine(line));
            }
        }

        if (ingredients.Count == 0 && steps.Count == 0 && lines.Count > 0)
        {
            // No clear segmentation at all (spec 003 SC-002): treat the whole thing as one step.
            steps.Add(string.Join(' ', lines.Select(StripListMarker)));
            title = string.Empty;
        }

        return new SegmentedRecipe(title, ingredients, steps);
    }

    private static string StripListMarker(string line) => ListMarkerPattern().Replace(line, string.Empty).Trim();

    private static bool LooksLikeStep(string line) =>
        line.Length > 60 || line.EndsWith('.') || line.EndsWith('!');

    private static bool LooksLikeIngredient(string line) =>
        LeadingQuantityPattern().IsMatch(line) || (line.Length <= 60 && !line.EndsWith('.'));

    private static DraftIngredientLine ParseIngredientLine(string line)
    {
        var match = IngredientLinePattern().Match(line);
        string quantity = string.Empty, unit = string.Empty, item = line, note = string.Empty;

        if (match.Success)
        {
            quantity = match.Groups["qty"].Value.Trim();
            unit = match.Groups["unit"].Value.Trim();
            item = match.Groups["item"].Value.Trim();
        }

        var commaIndex = item.IndexOf(',');
        if (commaIndex > 0 && commaIndex < item.Length - 1)
        {
            note = item[(commaIndex + 1)..].Trim();
            item = item[..commaIndex].Trim();
        }

        return new DraftIngredientLine(quantity, unit, item, note);
    }

    [GeneratedRegex(@"^\s*(?:[-*•]|\d+[\.\)])\s*")]
    private static partial Regex ListMarkerPattern();

    [GeneratedRegex(@"^\s*\d+(?:\.\d+)?(?:/\d+)?\s")]
    private static partial Regex LeadingQuantityPattern();

    [GeneratedRegex(@"^(?<qty>\d+(?:\.\d+)?(?:/\d+)?)\s*(?<unit>cups?|tbsp|tablespoons?|tsp|teaspoons?|oz|ounces?|lbs?|pounds?|g|grams?|kg|ml|l|liters?|cloves?|pinch(?:es)?)?\s+(?<item>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex IngredientLinePattern();
}
