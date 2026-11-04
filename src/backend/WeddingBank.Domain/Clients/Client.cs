namespace WeddingBank.Domain.Clients;

public sealed class Client
{
    private Client()
    {
    }

    public Guid Id { get; private set; }

    public string ClientReference { get; private set; } = "";

    public string LegalName { get; private set; } = "";

    public string? DisplayName { get; private set; }

    public string EmailAddress { get; private set; } = "";

    public string? PhoneNumber { get; private set; }

    public string? AddressLine1 { get; private set; }

    public string? AddressLine2 { get; private set; }

    public string? PostalCode { get; private set; }

    public string? City { get; private set; }

    public string? Country { get; private set; }

    public ClientStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == ClientStatus.Active;

    public static Client Open(
        Guid id,
        string clientReference,
        string legalName,
        string? displayName,
        string emailAddress,
        string? phoneNumber,
        string? addressLine1,
        string? addressLine2,
        string? postalCode,
        string? city,
        string? country,
        ClientStatus status,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A client needs an id.", nameof(id));
        }

        if (!ClientReferenceCode.IsCanonical(clientReference))
        {
            throw new ArgumentException("A client reference is not in the house format.", nameof(clientReference));
        }

        if (!ClientStatuses.TryParse(ClientStatuses.ToName(status), out _))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown client status.");
        }

        return new Client
        {
            Id = id,
            ClientReference = clientReference,
            LegalName = Require(legalName, ClientFields.LegalNameMax, nameof(legalName)),
            DisplayName = Optional(displayName, ClientFields.DisplayNameMax, nameof(displayName)),
            EmailAddress = RequireEmail(emailAddress),
            PhoneNumber = Optional(phoneNumber, ClientFields.PhoneMax, nameof(phoneNumber)),
            AddressLine1 = Optional(addressLine1, ClientFields.AddressMax, nameof(addressLine1)),
            AddressLine2 = Optional(addressLine2, ClientFields.AddressMax, nameof(addressLine2)),
            PostalCode = Optional(postalCode, ClientFields.PostalMax, nameof(postalCode)),
            City = Optional(city, ClientFields.CityMax, nameof(city)),
            Country = Optional(country, ClientFields.CountryMax, nameof(country)),
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public bool Revise(ClientRevision revision, DateTimeOffset now)
    {
        var changed = false;
        changed |= Assign(LegalName, revision.LegalName, revision.LegalNameSet, value => LegalName = value ?? "");
        changed |= Assign(DisplayName, revision.DisplayName, revision.DisplayNameSet, value => DisplayName = value);
        changed |= Assign(EmailAddress, revision.EmailAddress, revision.EmailAddressSet, value => EmailAddress = value ?? "");
        changed |= Assign(PhoneNumber, revision.PhoneNumber, revision.PhoneNumberSet, value => PhoneNumber = value);
        changed |= Assign(AddressLine1, revision.AddressLine1, revision.AddressLine1Set, value => AddressLine1 = value);
        changed |= Assign(AddressLine2, revision.AddressLine2, revision.AddressLine2Set, value => AddressLine2 = value);
        changed |= Assign(PostalCode, revision.PostalCode, revision.PostalCodeSet, value => PostalCode = value);
        changed |= Assign(City, revision.City, revision.CitySet, value => City = value);
        changed |= Assign(Country, revision.Country, revision.CountrySet, value => Country = value);
        if (changed)
        {
            UpdatedAt = now;
        }

        return changed;
    }

    public bool MoveTo(ClientStatus status, DateTimeOffset now)
    {
        if (!ClientStatuses.CanMove(Status, status))
        {
            return false;
        }

        Status = status;
        UpdatedAt = now;
        return true;
    }

    private static bool Assign(string? current, string? value, bool set, Action<string?> apply)
    {
        if (!set || current == value)
        {
            return false;
        }

        apply(value);
        return true;
    }

    private static string Require(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value != value.Trim())
        {
            throw new ArgumentException("A required client field is missing or too long.", name);
        }

        return value;
    }

    private static string? Optional(string? value, int max, string name)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value.Length > max || value != value.Trim())
        {
            throw new ArgumentException("An optional client field is empty or too long.", name);
        }

        return value;
    }

    private static string RequireEmail(string value)
    {
        if (!ClientFields.IsEmail(value))
        {
            throw new ArgumentException("A client email address is not valid.", nameof(value));
        }

        return value;
    }
}

public sealed class ClientRevision
{
    public string? LegalName { get; init; }

    public bool LegalNameSet { get; init; }

    public string? DisplayName { get; init; }

    public bool DisplayNameSet { get; init; }

    public string? EmailAddress { get; init; }

    public bool EmailAddressSet { get; init; }

    public string? PhoneNumber { get; init; }

    public bool PhoneNumberSet { get; init; }

    public string? AddressLine1 { get; init; }

    public bool AddressLine1Set { get; init; }

    public string? AddressLine2 { get; init; }

    public bool AddressLine2Set { get; init; }

    public string? PostalCode { get; init; }

    public bool PostalCodeSet { get; init; }

    public string? City { get; init; }

    public bool CitySet { get; init; }

    public string? Country { get; init; }

    public bool CountrySet { get; init; }

    public bool AnySet =>
        LegalNameSet || DisplayNameSet || EmailAddressSet || PhoneNumberSet
        || AddressLine1Set || AddressLine2Set || PostalCodeSet || CitySet || CountrySet;
}
