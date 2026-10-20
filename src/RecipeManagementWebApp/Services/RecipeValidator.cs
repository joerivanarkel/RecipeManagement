using RecipeManagementWebApp.Common;

namespace RecipeManagementWebApp.Services;

public record ValidatedIngredient(decimal? Quantity, string? Unit, string Item, string? Note);

public record ValidatedRecipeInput(
    string Title,
    int Servings,
    IReadOnlyList<ValidatedIngredient> Ingredients,
    IReadOnlyList<string> Steps);

/// <summary>Validates and normalizes raw form input per spec 001 FR-002..FR-007.</summary>
public static class RecipeValidator
{
    public const int TitleMaxLength = 200;
    public const int StepMaxLength = 1000;
    public const int ItemMaxLength = 200;
    public const int UnitMaxLength = 50;
    public const int NoteMaxLength = 300;

    public static Result<ValidatedRecipeInput> Validate(NewRecipeInput input)
    {
        var errors = new List<string>();

        var title = (input.Title ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(title))
        {
            errors.Add("Title is required");
        }
        else if (title.Length > TitleMaxLength)
        {
            errors.Add("Title is too long (max 200 characters)");
        }

        var servings = ValidateServings(input.Servings, errors);
        var ingredients = ValidateIngredients(input.Ingredients, errors);
        var steps = ValidateSteps(input.Steps, errors);

        if (errors.Count > 0)
        {
            return Result<ValidatedRecipeInput>.Failure(errors);
        }

        return Result<ValidatedRecipeInput>.Success(new ValidatedRecipeInput(title, servings, ingredients, steps));
    }

    private static int ValidateServings(string? rawServings, List<string> errors)
    {
        var text = (rawServings ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text) || !int.TryParse(text, out var servings))
        {
            errors.Add("Servings must be a valid number");
            return 0;
        }

        if (servings <= 0)
        {
            errors.Add("Servings must be greater than 0");
        }

        return servings;
    }

    private static List<ValidatedIngredient> ValidateIngredients(
        IReadOnlyList<NewIngredientInput> rawIngredients, List<string> errors)
    {
        var ingredients = new List<ValidatedIngredient>();

        foreach (var raw in rawIngredients)
        {
            var item = (raw.Item ?? string.Empty).Trim();
            var unit = string.IsNullOrWhiteSpace(raw.Unit) ? null : raw.Unit.Trim();
            var note = string.IsNullOrWhiteSpace(raw.Note) ? null : raw.Note.Trim();
            var quantityText = (raw.Quantity ?? string.Empty).Trim();

            var isBlankRow = item.Length == 0 && unit is null && note is null && quantityText.Length == 0;
            if (isBlankRow)
            {
                continue;
            }

            if (item.Length == 0)
            {
                errors.Add("Every ingredient must have an item");
                continue;
            }

            if (item.Length > ItemMaxLength)
            {
                errors.Add($"Ingredient item is too long (max {ItemMaxLength} characters)");
            }

            if (unit is not null && unit.Length > UnitMaxLength)
            {
                errors.Add($"Ingredient unit is too long (max {UnitMaxLength} characters)");
            }

            if (note is not null && note.Length > NoteMaxLength)
            {
                errors.Add($"Ingredient note is too long (max {NoteMaxLength} characters)");
            }

            decimal? quantity = null;
            if (quantityText.Length > 0)
            {
                if (!decimal.TryParse(quantityText, out var parsedQuantity))
                {
                    errors.Add($"Quantity for '{item}' must be a valid number");
                }
                else if (parsedQuantity <= 0)
                {
                    errors.Add($"Quantity for '{item}' must be greater than 0");
                }
                else
                {
                    quantity = parsedQuantity;
                }
            }

            ingredients.Add(new ValidatedIngredient(quantity, unit, item, note));
        }

        if (ingredients.Count == 0)
        {
            errors.Add("At least one ingredient is required");
        }

        return ingredients;
    }

    private static List<string> ValidateSteps(IReadOnlyList<string> rawSteps, List<string> errors)
    {
        var steps = new List<string>();

        foreach (var raw in rawSteps)
        {
            var text = (raw ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                // Whitespace-only steps are silently dropped rather than rejected (SC-012).
                continue;
            }

            if (text.Length > StepMaxLength)
            {
                errors.Add($"Step text is too long (max {StepMaxLength} characters)");
                continue;
            }

            steps.Add(text);
        }

        return steps;
    }
}
