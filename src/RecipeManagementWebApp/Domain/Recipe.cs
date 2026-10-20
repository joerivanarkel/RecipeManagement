namespace RecipeManagementWebApp.Domain;

public class Recipe
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Servings { get; set; }
    public List<Ingredient> Ingredients { get; set; } = [];
    public List<Step> Steps { get; set; } = [];
    public Guid? PhotoId { get; set; }
    public Photo? Photo { get; set; }
    public List<Tag> Tags { get; set; } = [];
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
