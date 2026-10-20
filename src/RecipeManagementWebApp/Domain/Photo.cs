namespace RecipeManagementWebApp.Domain;

public class Photo
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StoredAt { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
