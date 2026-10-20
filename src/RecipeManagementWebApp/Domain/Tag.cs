namespace RecipeManagementWebApp.Domain;

public class Tag
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
