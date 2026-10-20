namespace RecipeManagementWebApp.Domain;

public class CookLogEntry
{
    public Guid Id { get; set; }
    public Guid? RecipeId { get; set; }
    public DateTime CookedAt { get; set; }
    public int Servings { get; set; }
}
