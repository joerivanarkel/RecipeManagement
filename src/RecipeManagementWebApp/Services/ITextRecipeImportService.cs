using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public interface ITextRecipeImportService
{
    Result<ImportedRecipeDraft> Import(string rawText);
}

public class TextRecipeImportService : ITextRecipeImportService
{
    public Result<ImportedRecipeDraft> Import(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Result<ImportedRecipeDraft>.Failure("Paste some recipe text first");
        }

        var segmented = TextRecipeSegmenter.Segment(rawText);

        var draft = new ImportedRecipeDraft
        {
            SourceType = ImportSourceType.Text,
            RawText = rawText,
            Title = segmented.Title,
            Ingredients = [.. segmented.Ingredients],
            Steps = [.. segmented.Steps],
        };

        return Result<ImportedRecipeDraft>.Success(draft);
    }
}
