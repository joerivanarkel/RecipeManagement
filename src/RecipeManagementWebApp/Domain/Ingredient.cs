namespace RecipeManagementWebApp.Domain;

public class Ingredient
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string Item { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int DisplayOrder { get; set; }
}
