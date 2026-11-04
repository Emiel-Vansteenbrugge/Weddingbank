namespace WeddingBank.Application;

public enum OperationStatus
{
    Ok,
    Invalid,
    NotFound,
    Conflict,
}

public sealed class OperationResult<T>
    where T : class
{
    private OperationResult(
        OperationStatus status,
        T? value,
        string? title,
        IReadOnlyDictionary<string, string[]>? errors)
    {
        Status = status;
        Value = value;
        Title = title;
        Errors = errors;
    }

    public OperationStatus Status { get; }

    public T? Value { get; }

    public string? Title { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public static OperationResult<T> Ok(T value)
    {
        return new OperationResult<T>(OperationStatus.Ok, value, null, null);
    }

    public static OperationResult<T> NotFound(string title)
    {
        return new OperationResult<T>(OperationStatus.NotFound, null, title, null);
    }

    public static OperationResult<T> Conflict(string title)
    {
        return new OperationResult<T>(OperationStatus.Conflict, null, title, null);
    }

    public static OperationResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        return new OperationResult<T>(OperationStatus.Invalid, null, null, errors);
    }
}

public sealed record Completed;
