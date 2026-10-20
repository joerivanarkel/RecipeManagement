using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public record PhotoImportRequest(Stream Content, string ContentType, long Size);

public interface IPhotoRecipeImportService
{
    Task<Result<ImportedRecipeDraft>> ImportAsync(PhotoImportRequest request, CancellationToken cancellationToken = default);
}

public class PhotoRecipeImportService(IOcrEngine ocrEngine) : IPhotoRecipeImportService
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png",
    };

    public async Task<Result<ImportedRecipeDraft>> ImportAsync(PhotoImportRequest request, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result<ImportedRecipeDraft>.Failure("Please upload a JPG or PNG image");
        }

        if (request.Size > MaxFileSizeBytes)
        {
            return Result<ImportedRecipeDraft>.Failure("Image is too large (max 10MB)");
        }

        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        buffer.Position = 0;
        var ocrResult = await ocrEngine.ExtractTextAsync(buffer, cancellationToken);

        if (string.IsNullOrWhiteSpace(ocrResult.Text))
        {
            return Result<ImportedRecipeDraft>.Failure(
                "We couldn't read any text from that photo. Try a clearer photo or add manually.");
        }

        var segmented = TextRecipeSegmenter.Segment(ocrResult.Text);
        var dataUrl = $"data:{request.ContentType};base64,{Convert.ToBase64String(bytes)}";

        var draft = new ImportedRecipeDraft
        {
            SourceType = ImportSourceType.Photo,
            SourcePhotoDataUrl = dataUrl,
            OcrConfidence = ocrResult.Confidence,
            Title = segmented.Title,
            Ingredients = [.. segmented.Ingredients],
            Steps = [.. segmented.Steps],
        };

        return Result<ImportedRecipeDraft>.Success(draft);
    }
}
