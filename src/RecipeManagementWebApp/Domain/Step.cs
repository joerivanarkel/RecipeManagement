namespace RecipeManagementWebApp.Domain;

public class Step
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
