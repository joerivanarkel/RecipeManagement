namespace RecipeManagementWebApp.Common;

public class Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(bool succeeded, T? value, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, []);
    public static Result<T> Failure(params string[] errors) => new(false, default, errors);
    public static Result<T> Failure(IReadOnlyList<string> errors) => new(false, default, errors);
}
