namespace RecipeManagementWebApp.Services;

public enum ImportSourceType
{
    Url,
    Text,
    Photo,
}

public record DraftIngredientLine(string Quantity, string Unit, string Item, string Note);

/// <summary>An in-progress, unconfirmed recipe extracted from an external source (specs 002/003/004).
/// Never persisted directly -- it either becomes a real Recipe via spec 005's review screen, or is discarded.</summary>
public class ImportedRecipeDraft
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ImportSourceType SourceType { get; init; }
    public string? SourceUrl { get; init; }
    public string? RawText { get; init; }

    /// <summary>Base64 data URL for displaying the uploaded photo during review; the file itself is only
    /// written to durable storage if the user keeps it as the recipe's photo on save.</summary>
    public string? SourcePhotoDataUrl { get; init; }

    public double? OcrConfidence { get; init; }

    public string Title { get; set; } = string.Empty;
    public string Servings { get; set; } = string.Empty;
    public List<DraftIngredientLine> Ingredients { get; set; } = [];
    public List<string> Steps { get; set; } = [];

    /// <summary>Photo URL extracted from structured markup (URL imports only) -- reference only, not auto-downloaded.</summary>
    public string? ExtractedPhotoUrl { get; init; }
}
