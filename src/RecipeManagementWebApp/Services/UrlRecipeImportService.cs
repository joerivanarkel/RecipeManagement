using System.Text.Json;
using System.Text.RegularExpressions;
using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public partial class UrlRecipeImportService(HttpClient httpClient) : IUrlRecipeImportService
{
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);

    public async Task<Result<ImportedRecipeDraft>> ImportAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result<ImportedRecipeDraft>.Failure("Please enter a valid URL");
        }

        string html;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(FetchTimeout);

            using var response = await httpClient.GetAsync(uri, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return Result<ImportedRecipeDraft>.Failure("Couldn't reach that page. Check the link and try again.");
            }

            html = await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<ImportedRecipeDraft>.Failure("That page took too long to load. Try again or add manually.");
        }
        catch (HttpRequestException)
        {
            return Result<ImportedRecipeDraft>.Failure("Couldn't reach that page. Check the link and try again.");
        }

        var draft = TryExtractJsonLdRecipe(html, url) ?? TryExtractHeuristically(html, url);
        if (draft is null)
        {
            return Result<ImportedRecipeDraft>.Failure("We couldn't find a recipe on that page.");
        }

        return Result<ImportedRecipeDraft>.Success(draft);
    }

    private static ImportedRecipeDraft? TryExtractJsonLdRecipe(string html, string sourceUrl)
    {
        foreach (Match scriptMatch in JsonLdScriptPattern().Matches(html))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(scriptMatch.Groups["json"].Value);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var recipeElement = FindRecipeElement(document.RootElement);
                if (recipeElement is { } recipe)
                {
                    return BuildDraftFromJsonLd(recipe, sourceUrl);
                }
            }
        }

        return null;
    }

    private static JsonElement? FindRecipeElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("@type", out var typeProp) && IsRecipeType(typeProp))
            {
                return element;
            }

            if (element.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in graph.EnumerateArray())
                {
                    if (FindRecipeElement(item) is { } found)
                    {
                        return found;
                    }
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (FindRecipeElement(item) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static bool IsRecipeType(JsonElement typeProp)
    {
        if (typeProp.ValueKind == JsonValueKind.String)
        {
            return string.Equals(typeProp.GetString(), "Recipe", StringComparison.OrdinalIgnoreCase);
        }

        if (typeProp.ValueKind == JsonValueKind.Array)
        {
            return typeProp.EnumerateArray()
                .Any(t => t.ValueKind == JsonValueKind.String && string.Equals(t.GetString(), "Recipe", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private static ImportedRecipeDraft BuildDraftFromJsonLd(JsonElement recipe, string sourceUrl)
    {
        var title = GetString(recipe, "name") ?? string.Empty;

        var ingredients = new List<DraftIngredientLine>();
        if (recipe.TryGetProperty("recipeIngredient", out var ingredientsProp) && ingredientsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in ingredientsProp.EnumerateArray())
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ingredients.Add(TextRecipeSegmenter.Segment(text).Ingredients.FirstOrDefault()
                        ?? new DraftIngredientLine(string.Empty, string.Empty, text.Trim(), string.Empty));
                }
            }
        }

        var steps = new List<string>();
        if (recipe.TryGetProperty("recipeInstructions", out var instructionsProp))
        {
            steps.AddRange(ExtractInstructionTexts(instructionsProp));
        }

        int? servings = null;
        if (recipe.TryGetProperty("recipeYield", out var yieldProp))
        {
            var yieldText = yieldProp.ValueKind switch
            {
                JsonValueKind.String => yieldProp.GetString(),
                JsonValueKind.Number => yieldProp.GetRawText(),
                _ => null,
            };
            if (yieldText is not null)
            {
                var digits = LeadingDigitsPattern().Match(yieldText);
                if (digits.Success && int.TryParse(digits.Value, out var parsedYield))
                {
                    servings = parsedYield;
                }
            }
        }

        string? photoUrl = null;
        if (recipe.TryGetProperty("image", out var imageProp))
        {
            photoUrl = imageProp.ValueKind switch
            {
                JsonValueKind.String => imageProp.GetString(),
                JsonValueKind.Array when imageProp.GetArrayLength() > 0 => imageProp[0].ValueKind == JsonValueKind.String
                    ? imageProp[0].GetString()
                    : GetString(imageProp[0], "url"),
                JsonValueKind.Object => GetString(imageProp, "url"),
                _ => null,
            };
        }

        return new ImportedRecipeDraft
        {
            SourceType = ImportSourceType.Url,
            SourceUrl = sourceUrl,
            Title = title,
            Servings = servings?.ToString() ?? string.Empty,
            Ingredients = ingredients,
            Steps = steps,
            ExtractedPhotoUrl = photoUrl,
        };
    }

    private static IEnumerable<string> ExtractInstructionTexts(JsonElement instructionsProp)
    {
        switch (instructionsProp.ValueKind)
        {
            case JsonValueKind.String:
                var text = instructionsProp.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return TextRecipeSegmenter.Segment(text).Steps;
                }

                return [];

            case JsonValueKind.Array:
                var results = new List<string>();
                foreach (var item in instructionsProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            results.Add(s.Trim());
                        }
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        var stepText = GetString(item, "text");
                        if (!string.IsNullOrWhiteSpace(stepText))
                        {
                            results.Add(stepText.Trim());
                        }
                    }
                }

                return results;

            default:
                return [];
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static ImportedRecipeDraft? TryExtractHeuristically(string html, string sourceUrl)
    {
        var withoutScripts = ScriptOrStylePattern().Replace(html, " ");
        var plainText = HtmlTagPattern().Replace(withoutScripts, "\n");
        plainText = System.Net.WebUtility.HtmlDecode(plainText);

        var segmented = TextRecipeSegmenter.Segment(plainText);
        if (segmented.Ingredients.Count == 0 && segmented.Steps.Count == 0)
        {
            return null;
        }

        return new ImportedRecipeDraft
        {
            SourceType = ImportSourceType.Url,
            SourceUrl = sourceUrl,
            Title = segmented.Title,
            Ingredients = [.. segmented.Ingredients],
            Steps = [.. segmented.Steps],
        };
    }

    [GeneratedRegex(@"<script[^>]+type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptPattern();

    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStylePattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingDigitsPattern();
}
