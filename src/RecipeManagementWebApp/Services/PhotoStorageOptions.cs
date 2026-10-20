namespace RecipeManagementWebApp.Services;

public class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    public string RootPath { get; set; } = "wwwroot/uploads";
    public string RequestPathPrefix { get; set; } = "/uploads";
}
