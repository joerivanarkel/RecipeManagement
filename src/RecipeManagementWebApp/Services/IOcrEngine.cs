namespace RecipeManagementWebApp.Services;

public record OcrResult(string Text, double Confidence);

/// <summary>Seam for plugging in a real OCR provider. Spec 004's own open question #1 defers the
/// on-device-vs-cloud decision to architecture; <see cref="NullOcrEngine"/> is an honest placeholder,
/// not a real implementation, so photo imports currently always report "no text found" rather than
/// silently fabricating results.</summary>
public interface IOcrEngine
{
    Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default);
}

public class NullOcrEngine : IOcrEngine
{
    public Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default) =>
        Task.FromResult(new OcrResult(string.Empty, 0));
}
