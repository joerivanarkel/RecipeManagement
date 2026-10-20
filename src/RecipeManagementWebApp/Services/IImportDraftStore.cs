namespace RecipeManagementWebApp.Services;

/// <summary>Holds the in-progress import draft between the import page and the review page (spec 005).
/// Scoped per Blazor Server circuit, matching the domain rule that drafts are ephemeral and never persisted.</summary>
public interface IImportDraftStore
{
    ImportedRecipeDraft? Current { get; set; }
}

public class ImportDraftStore : IImportDraftStore
{
    public ImportedRecipeDraft? Current { get; set; }
}
