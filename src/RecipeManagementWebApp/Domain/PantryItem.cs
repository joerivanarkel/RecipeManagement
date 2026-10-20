namespace RecipeManagementWebApp.Domain;

public enum PantryCategory
{
    Fridge,
    Freezer,
    Cupboards,
}

public class PantryItem
{
    public Guid Id { get; set; }
    public PantryCategory Category { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
