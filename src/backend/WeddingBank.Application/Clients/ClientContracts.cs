using WeddingBank.Domain.Clients;

namespace WeddingBank.Application.Clients;

public interface IClientReferenceSource
{
    string Create();
}

public enum ClientConflictKind
{
    Reference,
    Link,
}

public sealed class ClientWriteConflictException(ClientConflictKind kind) : Exception
{
    public ClientConflictKind Kind { get; } = kind;
}

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

public sealed record AccountProfile(Guid Id, string Login, string DisplayName, IReadOnlyList<string> Roles);

public sealed record LinkedAccount(
    Guid UserId,
    string Login,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTimeOffset LinkedAt);

public sealed record ClientSummary(
    Guid Id,
    string ClientReference,
    string LegalName,
    string? DisplayName,
    string EmailAddress,
    string? City,
    string? Country,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ClientDetail(
    Guid Id,
    string ClientReference,
    string LegalName,
    string? DisplayName,
    string EmailAddress,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    string? City,
    string? Country,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OwnProfile(string Login, string DisplayName);

public sealed record OwnRelationship(
    Guid Id,
    string ClientReference,
    string LegalName,
    string? DisplayName,
    string Status,
    bool Active,
    string? EmailAddress,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    string? City,
    string? Country);

public sealed record CreateClient(
    string? LegalName,
    string? DisplayName,
    string? EmailAddress,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? PostalCode,
    string? City,
    string? Country,
    string? Status);

public sealed record ClientListQuery(string? Search, ClientStatus? Status, int Page, int PageSize);

public sealed record ClientListSlice(
    int Total,
    int Active,
    int Inactive,
    int Suspended,
    int FilteredCount,
    IReadOnlyList<Client> Items);
