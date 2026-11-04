using WeddingBank.Application.Identity;
using WeddingBank.Domain.Clients;
using WeddingBank.Domain.Identity;

namespace WeddingBank.Application.Clients;

public sealed class ClientService(
    IClientStore clients,
    IIdentityStore identities,
    IClientReferenceSource references,
    TimeProvider clock)
{
    public async Task<ClientListSlice> ListAsync(ClientListQuery query, CancellationToken cancellationToken)
    {
        return await clients.ListClientsAsync(query, cancellationToken);
    }

    public async Task<ClientDetail?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await clients.FindClientAsync(id, cancellationToken);
        return client is null ? null : ToDetail(client);
    }

    public async Task<OperationResult<ClientDetail>> CreateAsync(
        CreateClient request,
        Guid actorId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var legalName = ClientFields.OneLine(request.LegalName);
        var email = ClientFields.OneLine(request.EmailAddress);
        var displayName = ClientFields.OneLine(request.DisplayName);
        var phone = ClientFields.OneLine(request.PhoneNumber);
        var addressLine1 = ClientFields.OneLine(request.AddressLine1);
        var addressLine2 = ClientFields.OneLine(request.AddressLine2);
        var postalCode = ClientFields.OneLine(request.PostalCode);
        var city = ClientFields.OneLine(request.City);
        var country = ClientFields.OneLine(request.Country);

        RequireText(errors, "legalName", legalName, ClientFields.LegalNameMax, "Enter a legal name.");
        RequireEmail(errors, email);
        OptionalText(errors, "displayName", displayName, ClientFields.DisplayNameMax, "Enter a shorter display name.");
        OptionalText(errors, "phoneNumber", phone, ClientFields.PhoneMax, "Enter a shorter telephone number.");
        OptionalText(errors, "addressLine1", addressLine1, ClientFields.AddressMax, "Enter a shorter address.");
        OptionalText(errors, "addressLine2", addressLine2, ClientFields.AddressMax, "Enter a shorter address.");
        OptionalText(errors, "postalCode", postalCode, ClientFields.PostalMax, "Enter a shorter postal code.");
        OptionalText(errors, "city", city, ClientFields.CityMax, "Enter a shorter city.");
        OptionalText(errors, "country", country, ClientFields.CountryMax, "Enter a shorter country.");

        if (!ClientStatuses.TryParse(request.Status, out var status))
        {
            errors["status"] = ["Choose a status."];
        }

        if (errors.Count > 0)
        {
            return OperationResult<ClientDetail>.Invalid(errors);
        }

        var reference = await ReserveReferenceAsync(cancellationToken);
        if (reference is null)
        {
            return OperationResult<ClientDetail>.Conflict("A client reference could not be reserved. Submit the client again.");
        }

        var now = clock.GetUtcNow();
        var client = Client.Open(
            Guid.CreateVersion7(),
            reference,
            legalName!,
            displayName,
            email!,
            phone,
            addressLine1,
            addressLine2,
            postalCode,
            city,
            country,
            status,
            now);
        clients.AddClient(client);
        identities.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            actorId,
            AuditEvent.ClientCreated,
            "client",
            client.Id.ToString(),
            ipAddress,
            new Dictionary<string, string>
            {
                ["reference"] = client.ClientReference,
                ["status"] = ClientStatuses.ToName(client.Status),
            }));

        try
        {
            await clients.SaveChangesAsync(cancellationToken);
        }
        catch (ClientWriteConflictException)
        {
            return OperationResult<ClientDetail>.Conflict("A client reference could not be reserved. Submit the client again.");
        }

        return OperationResult<ClientDetail>.Ok(ToDetail(client));
    }

    public async Task<OperationResult<ClientDetail>> UpdateAsync(
        Guid id,
        ClientRevision revision,
        Guid actorId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        revision = Clean(revision);
        if (!revision.AnySet)
        {
            return OperationResult<ClientDetail>.Invalid(One("client", "Submit a change."));
        }

        var errors = new Dictionary<string, string[]>();
        if (revision.LegalNameSet)
        {
            RequireText(errors, "legalName", revision.LegalName, ClientFields.LegalNameMax, "Enter a legal name.");
        }

        if (revision.EmailAddressSet)
        {
            RequireEmail(errors, revision.EmailAddress);
        }

        OptionalText(errors, "displayName", revision.DisplayName, ClientFields.DisplayNameMax, "Enter a shorter display name.", revision.DisplayNameSet);
        OptionalText(errors, "phoneNumber", revision.PhoneNumber, ClientFields.PhoneMax, "Enter a shorter telephone number.", revision.PhoneNumberSet);
        OptionalText(errors, "addressLine1", revision.AddressLine1, ClientFields.AddressMax, "Enter a shorter address.", revision.AddressLine1Set);
        OptionalText(errors, "addressLine2", revision.AddressLine2, ClientFields.AddressMax, "Enter a shorter address.", revision.AddressLine2Set);
        OptionalText(errors, "postalCode", revision.PostalCode, ClientFields.PostalMax, "Enter a shorter postal code.", revision.PostalCodeSet);
        OptionalText(errors, "city", revision.City, ClientFields.CityMax, "Enter a shorter city.", revision.CitySet);
        OptionalText(errors, "country", revision.Country, ClientFields.CountryMax, "Enter a shorter country.", revision.CountrySet);
        if (errors.Count > 0)
        {
            return OperationResult<ClientDetail>.Invalid(errors);
        }

        var client = await clients.FindClientAsync(id, cancellationToken);
        if (client is null)
        {
            return OperationResult<ClientDetail>.NotFound("This client record was not found.");
        }

        var fields = ChangedFields(client, revision);
        if (fields.Count == 0)
        {
            return OperationResult<ClientDetail>.Ok(ToDetail(client));
        }

        var now = clock.GetUtcNow();
        client.Revise(revision, now);
        identities.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            actorId,
            AuditEvent.ClientUpdated,
            "client",
            client.Id.ToString(),
            ipAddress,
            new Dictionary<string, string> { ["fields"] = string.Join(',', fields) }));
        await clients.SaveChangesAsync(cancellationToken);
        return OperationResult<ClientDetail>.Ok(ToDetail(client));
    }

    public async Task<OperationResult<ClientDetail>> ChangeStatusAsync(
        Guid id,
        string? statusName,
        Guid actorId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (!ClientStatuses.TryParse(statusName, out var status))
        {
            return OperationResult<ClientDetail>.Invalid(One("status", "Choose a status."));
        }

        var client = await clients.FindClientAsync(id, cancellationToken);
        if (client is null)
        {
            return OperationResult<ClientDetail>.NotFound("This client record was not found.");
        }

        if (client.Status == status)
        {
            return OperationResult<ClientDetail>.Invalid(One("status", "The relationship already has this status."));
        }

        var previous = ClientStatuses.ToName(client.Status);
        var now = clock.GetUtcNow();
        if (!client.MoveTo(status, now))
        {
            return OperationResult<ClientDetail>.Invalid(One("status", "That status change is not available for this relationship."));
        }

        identities.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            actorId,
            AuditEvent.ClientStatusChanged,
            "client",
            client.Id.ToString(),
            ipAddress,
            new Dictionary<string, string>
            {
                ["fromStatus"] = previous,
                ["toStatus"] = ClientStatuses.ToName(status),
            }));
        await clients.SaveChangesAsync(cancellationToken);
        return OperationResult<ClientDetail>.Ok(ToDetail(client));
    }

    public async Task<IReadOnlyList<LinkedAccount>?> ListAccountsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await clients.FindClientAsync(clientId, cancellationToken);
        if (client is null)
        {
            return null;
        }

        return await clients.ListLinkedAccountsAsync(clientId, cancellationToken);
    }

    public async Task<OperationResult<LinkedAccount>> LinkAsync(
        Guid clientId,
        string? login,
        Guid actorId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindClientAsync(clientId, cancellationToken);
        if (client is null)
        {
            return OperationResult<LinkedAccount>.NotFound("This client record was not found.");
        }

        var normalized = LoginName.Normalize(login);
        if (normalized is null)
        {
            return OperationResult<LinkedAccount>.Invalid(One("login", "Enter the login of an existing account."));
        }

        var account = await clients.FindAccountByLoginAsync(normalized, cancellationToken);
        if (account is null)
        {
            return OperationResult<LinkedAccount>.NotFound("No account matches that login.");
        }

        if (await clients.LinkExistsAsync(client.Id, account.Id, cancellationToken))
        {
            return OperationResult<LinkedAccount>.Conflict("That account is already linked to this client.");
        }

        var now = clock.GetUtcNow();
        var link = ClientUser.Link(client.Id, account.Id, now);
        clients.AddLink(link);
        identities.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            actorId,
            AuditEvent.ClientUserLinked,
            "client",
            client.Id.ToString(),
            ipAddress,
            new Dictionary<string, string> { ["userId"] = account.Id.ToString() }));

        try
        {
            await clients.SaveChangesAsync(cancellationToken);
        }
        catch (ClientWriteConflictException exception) when (exception.Kind == ClientConflictKind.Link)
        {
            return OperationResult<LinkedAccount>.Conflict("That account is already linked to this client.");
        }

        var linked = new LinkedAccount(account.Id, account.Login, account.DisplayName, account.Roles, now);
        return OperationResult<LinkedAccount>.Ok(linked);
    }

    public async Task<OperationResult<Completed>> UnlinkAsync(
        Guid clientId,
        Guid userId,
        Guid actorId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindClientAsync(clientId, cancellationToken);
        if (client is null)
        {
            return OperationResult<Completed>.NotFound("This client record was not found.");
        }

        var link = await clients.FindLinkAsync(clientId, userId, cancellationToken);
        if (link is null)
        {
            return OperationResult<Completed>.NotFound("That account is not linked to this client.");
        }

        var now = clock.GetUtcNow();
        clients.RemoveLink(link);
        identities.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            actorId,
            AuditEvent.ClientUserUnlinked,
            "client",
            client.Id.ToString(),
            ipAddress,
            new Dictionary<string, string> { ["userId"] = userId.ToString() }));
        await clients.SaveChangesAsync(cancellationToken);
        return OperationResult<Completed>.Ok(new Completed());
    }

    public async Task<OwnProfile?> OwnProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await clients.FindAccountByIdAsync(userId, cancellationToken);
        return account is null ? null : new OwnProfile(account.Login, account.DisplayName);
    }

    public async Task<IReadOnlyList<OwnRelationship>> ListOwnAsync(Guid userId, CancellationToken cancellationToken)
    {
        var records = await clients.ListClientsForUserAsync(userId, cancellationToken);
        return records.Select(ToOwn).ToArray();
    }

    public async Task<OwnRelationship?> GetOwnAsync(Guid userId, Guid clientId, CancellationToken cancellationToken)
    {
        if (!await clients.LinkExistsAsync(clientId, userId, cancellationToken))
        {
            return null;
        }

        var client = await clients.FindClientAsync(clientId, cancellationToken);
        return client is null ? null : ToOwn(client);
    }

    private async Task<string?> ReserveReferenceAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = references.Create();
            if (!ClientReferenceCode.IsCanonical(candidate))
            {
                continue;
            }

            if (!await clients.ReferenceInUseAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return null;
    }

    private static ClientRevision Clean(ClientRevision revision)
    {
        return new ClientRevision
        {
            LegalNameSet = revision.LegalNameSet,
            LegalName = revision.LegalNameSet ? ClientFields.OneLine(revision.LegalName) : null,
            DisplayNameSet = revision.DisplayNameSet,
            DisplayName = revision.DisplayNameSet ? ClientFields.OneLine(revision.DisplayName) : null,
            EmailAddressSet = revision.EmailAddressSet,
            EmailAddress = revision.EmailAddressSet ? ClientFields.OneLine(revision.EmailAddress) : null,
            PhoneNumberSet = revision.PhoneNumberSet,
            PhoneNumber = revision.PhoneNumberSet ? ClientFields.OneLine(revision.PhoneNumber) : null,
            AddressLine1Set = revision.AddressLine1Set,
            AddressLine1 = revision.AddressLine1Set ? ClientFields.OneLine(revision.AddressLine1) : null,
            AddressLine2Set = revision.AddressLine2Set,
            AddressLine2 = revision.AddressLine2Set ? ClientFields.OneLine(revision.AddressLine2) : null,
            PostalCodeSet = revision.PostalCodeSet,
            PostalCode = revision.PostalCodeSet ? ClientFields.OneLine(revision.PostalCode) : null,
            CitySet = revision.CitySet,
            City = revision.CitySet ? ClientFields.OneLine(revision.City) : null,
            CountrySet = revision.CountrySet,
            Country = revision.CountrySet ? ClientFields.OneLine(revision.Country) : null,
        };
    }

    private static List<string> ChangedFields(Client client, ClientRevision revision)
    {
        var fields = new List<string>();
        Note(fields, "legalName", revision.LegalNameSet, client.LegalName, revision.LegalName);
        Note(fields, "displayName", revision.DisplayNameSet, client.DisplayName, revision.DisplayName);
        Note(fields, "emailAddress", revision.EmailAddressSet, client.EmailAddress, revision.EmailAddress);
        Note(fields, "phoneNumber", revision.PhoneNumberSet, client.PhoneNumber, revision.PhoneNumber);
        Note(fields, "addressLine1", revision.AddressLine1Set, client.AddressLine1, revision.AddressLine1);
        Note(fields, "addressLine2", revision.AddressLine2Set, client.AddressLine2, revision.AddressLine2);
        Note(fields, "postalCode", revision.PostalCodeSet, client.PostalCode, revision.PostalCode);
        Note(fields, "city", revision.CitySet, client.City, revision.City);
        Note(fields, "country", revision.CountrySet, client.Country, revision.Country);
        return fields;
    }

    private static void Note(List<string> fields, string name, bool set, string? current, string? next)
    {
        if (set && current != next)
        {
            fields.Add(name);
        }
    }

    private static void RequireText(
        Dictionary<string, string[]> errors,
        string field,
        string? value,
        int max,
        string missing)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [missing];
            return;
        }

        if (value.Length > max)
        {
            errors[field] = ["Enter a shorter value."];
        }
    }

    private static void RequireEmail(Dictionary<string, string[]> errors, string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !ClientFields.IsEmail(email))
        {
            errors["emailAddress"] = ["Enter an email address."];
        }
    }

    private static void OptionalText(
        Dictionary<string, string[]> errors,
        string field,
        string? value,
        int max,
        string message,
        bool set = true)
    {
        if (set && value is not null && value.Length > max)
        {
            errors[field] = [message];
        }
    }

    private static Dictionary<string, string[]> One(string field, string message)
    {
        return new Dictionary<string, string[]> { [field] = [message] };
    }

    private static ClientDetail ToDetail(Client client)
    {
        return new ClientDetail(
            client.Id,
            client.ClientReference,
            client.LegalName,
            client.DisplayName,
            client.EmailAddress,
            client.PhoneNumber,
            client.AddressLine1,
            client.AddressLine2,
            client.PostalCode,
            client.City,
            client.Country,
            ClientStatuses.ToName(client.Status),
            client.CreatedAt,
            client.UpdatedAt);
    }

    private static OwnRelationship ToOwn(Client client)
    {
        var active = client.IsActive;
        return new OwnRelationship(
            client.Id,
            client.ClientReference,
            client.LegalName,
            client.DisplayName,
            ClientStatuses.ToName(client.Status),
            active,
            active ? client.EmailAddress : null,
            active ? client.PhoneNumber : null,
            active ? client.AddressLine1 : null,
            active ? client.AddressLine2 : null,
            active ? client.PostalCode : null,
            active ? client.City : null,
            active ? client.Country : null);
    }
}
