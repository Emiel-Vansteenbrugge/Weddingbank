using System.Security.Claims;
using System.Text.Json;
using WeddingBank.Api.Identity;
using WeddingBank.Application.Clients;
using WeddingBank.Domain.Clients;

namespace WeddingBank.Api.Clients;

internal static class ClientEndpoints
{
    public static void MapClientEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/clients").RequireAuthorization("Admin");
        admin.MapGet("", ListAsync);
        admin.MapGet("/{id:guid}", GetAsync);
        admin.MapPost("", CreateAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPatch("/{id:guid}", UpdateAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/status", ChangeStatusAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapGet("/{id:guid}/users", ListUsersAsync);
        admin.MapPost("/{id:guid}/users", LinkAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapDelete("/{id:guid}/users/{userId:guid}", UnlinkAsync).AddEndpointFilter(HouseRequestGuard.Invoke);

        var own = app.MapGroup("/api/client").RequireAuthorization("Client");
        own.MapGet("/profile", ProfileAsync);
        own.MapGet("/relationships", ListOwnAsync);
        own.MapGet("/relationships/{id:guid}", GetOwnAsync);
    }

    private static async Task<IResult> ListAsync(
        string? search,
        string? status,
        int? page,
        int? pageSize,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (search is not null && search.Trim().Length > 80)
        {
            errors["search"] = ["Enter a shorter search."];
        }

        ClientStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ClientStatuses.TryParse(status, out var value))
            {
                errors["status"] = ["Choose a status."];
            }
            else
            {
                parsed = value;
            }
        }

        var resolvedPage = page ?? 1;
        var resolvedSize = pageSize ?? 20;
        if (resolvedPage < 1)
        {
            errors["page"] = ["Choose a page."];
        }

        if (resolvedSize is < 1 or > 50)
        {
            errors["pageSize"] = ["Choose a page size between 1 and 50."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var slice = await clients.ListAsync(
            new ClientListQuery(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), parsed, resolvedPage, resolvedSize),
            cancellationToken);
        return Results.Ok(new
        {
            total = slice.Total,
            active = slice.Active,
            inactive = slice.Inactive,
            suspended = slice.Suspended,
            filteredCount = slice.FilteredCount,
            page = resolvedPage,
            pageSize = resolvedSize,
            items = slice.Items.Select(Summary).ToArray(),
        });
    }

    private static async Task<IResult> GetAsync(Guid id, ClientService clients, CancellationToken cancellationToken)
    {
        var client = await clients.GetAsync(id, cancellationToken);
        return client is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "This client record was not found.")
            : Results.Ok(client);
    }

    private static async Task<IResult> CreateAsync(
        CreateClientRequest request,
        ClaimsPrincipal principal,
        HttpContext http,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        var result = await clients.CreateAsync(
            new CreateClient(
                request.LegalName,
                request.DisplayName,
                request.EmailAddress,
                request.PhoneNumber,
                request.AddressLine1,
                request.AddressLine2,
                request.PostalCode,
                request.City,
                request.Country,
                request.Status),
            actorId,
            HouseRequestGuard.ClientIp(http),
            cancellationToken);
        return From(result, value => Results.Created($"/api/admin/clients/{value.Id}", value));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        HttpRequest request,
        ClaimsPrincipal principal,
        HttpContext http,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        JsonElement body;
        try
        {
            body = await request.ReadFromJsonAsync<JsonElement>(cancellationToken);
        }
        catch (JsonException)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["client"] = ["Submit the client fields."] });
        }

        var read = ClientPatchReader.Read(body);
        if (read.Errors is not null)
        {
            return Results.ValidationProblem(read.Errors);
        }

        var result = await clients.UpdateAsync(id, read.Revision!, actorId, HouseRequestGuard.ClientIp(http), cancellationToken);
        return From(result, value => Results.Ok(value));
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        StatusRequest request,
        ClaimsPrincipal principal,
        HttpContext http,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        var result = await clients.ChangeStatusAsync(id, request.Status, actorId, HouseRequestGuard.ClientIp(http), cancellationToken);
        return From(result, value => Results.Ok(value));
    }

    private static async Task<IResult> ListUsersAsync(Guid id, ClientService clients, CancellationToken cancellationToken)
    {
        var accounts = await clients.ListAccountsAsync(id, cancellationToken);
        return accounts is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "This client record was not found.")
            : Results.Ok(accounts);
    }

    private static async Task<IResult> LinkAsync(
        Guid id,
        LinkRequest request,
        ClaimsPrincipal principal,
        HttpContext http,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        var result = await clients.LinkAsync(id, request.Login, actorId, HouseRequestGuard.ClientIp(http), cancellationToken);
        return From(result, value => Results.Ok(value));
    }

    private static async Task<IResult> UnlinkAsync(
        Guid id,
        Guid userId,
        ClaimsPrincipal principal,
        HttpContext http,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        var result = await clients.UnlinkAsync(id, userId, actorId, HouseRequestGuard.ClientIp(http), cancellationToken);
        return From(result, _ => Results.NoContent());
    }

    private static IResult ProfileAsync(ClaimsPrincipal principal)
    {
        var login = principal.FindFirstValue(HouseAuthenticationExtensions.LoginClaim);
        var displayName = principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(displayName))
        {
            return Unauthorized();
        }

        return Results.Ok(new OwnProfile(login, displayName));
    }

    private static async Task<IResult> ListOwnAsync(
        ClaimsPrincipal principal,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        var relationships = await clients.ListOwnAsync(actorId, cancellationToken);
        return Results.Ok(relationships);
    }

    private static async Task<IResult> GetOwnAsync(
        Guid id,
        ClaimsPrincipal principal,
        ClientService clients,
        CancellationToken cancellationToken)
    {
        if (!TryActor(principal, out var actorId))
        {
            return Unauthorized();
        }

        var relationship = await clients.GetOwnAsync(actorId, id, cancellationToken);
        return relationship is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "This relationship is not open to you.")
            : Results.Ok(relationship);
    }

    private static bool TryActor(ClaimsPrincipal principal, out Guid id)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out id) && id != Guid.Empty;
    }

    private static IResult Unauthorized()
    {
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.");
    }

    private static IResult From<T>(OperationResult<T> result, Func<T, IResult> ok)
        where T : class
    {
        return result.Status switch
        {
            OperationStatus.Ok => ok(result.Value!),
            OperationStatus.NotFound => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: result.Title),
            OperationStatus.Conflict => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: result.Title),
            OperationStatus.Invalid => Results.ValidationProblem(new Dictionary<string, string[]>(result.Errors!)),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "The request could not be completed."),
        };
    }

    private static object Summary(WeddingBank.Domain.Clients.Client client)
    {
        return new
        {
            id = client.Id,
            clientReference = client.ClientReference,
            legalName = client.LegalName,
            displayName = client.DisplayName,
            emailAddress = client.EmailAddress,
            city = client.City,
            country = client.Country,
            status = ClientStatuses.ToName(client.Status),
            createdAt = client.CreatedAt,
        };
    }
}

internal static class ClientPatchReader
{
    public static (ClientRevision? Revision, Dictionary<string, string[]>? Errors) Read(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
        {
            return (null, new Dictionary<string, string[]> { ["client"] = ["Submit the client fields."] });
        }

        var errors = new Dictionary<string, string[]>();
        var legalNameSet = false;
        string? legalName = null;
        var displayNameSet = false;
        string? displayName = null;
        var emailSet = false;
        string? email = null;
        var phoneSet = false;
        string? phone = null;
        var line1Set = false;
        string? line1 = null;
        var line2Set = false;
        string? line2 = null;
        var postalSet = false;
        string? postal = null;
        var citySet = false;
        string? city = null;
        var countrySet = false;
        string? country = null;

        foreach (var property in body.EnumerateObject())
        {
            var known = Known(property.Name);
            if (known is null)
            {
                errors[property.Name] = ["This field cannot be changed here."];
                continue;
            }

            if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
            {
                errors[known] = ["Enter text."];
                continue;
            }

            var value = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
            switch (known)
            {
                case "legalName":
                    legalNameSet = true;
                    legalName = value;
                    break;
                case "displayName":
                    displayNameSet = true;
                    displayName = value;
                    break;
                case "emailAddress":
                    emailSet = true;
                    email = value;
                    break;
                case "phoneNumber":
                    phoneSet = true;
                    phone = value;
                    break;
                case "addressLine1":
                    line1Set = true;
                    line1 = value;
                    break;
                case "addressLine2":
                    line2Set = true;
                    line2 = value;
                    break;
                case "postalCode":
                    postalSet = true;
                    postal = value;
                    break;
                case "city":
                    citySet = true;
                    city = value;
                    break;
                case "country":
                    countrySet = true;
                    country = value;
                    break;
            }
        }

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        return (new ClientRevision
        {
            LegalNameSet = legalNameSet,
            LegalName = legalName,
            DisplayNameSet = displayNameSet,
            DisplayName = displayName,
            EmailAddressSet = emailSet,
            EmailAddress = email,
            PhoneNumberSet = phoneSet,
            PhoneNumber = phone,
            AddressLine1Set = line1Set,
            AddressLine1 = line1,
            AddressLine2Set = line2Set,
            AddressLine2 = line2,
            PostalCodeSet = postalSet,
            PostalCode = postal,
            CitySet = citySet,
            City = city,
            CountrySet = countrySet,
            Country = country,
        }, null);
    }

    private static string? Known(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "legalname" => "legalName",
            "displayname" => "displayName",
            "emailaddress" => "emailAddress",
            "phonenumber" => "phoneNumber",
            "addressline1" => "addressLine1",
            "addressline2" => "addressLine2",
            "postalcode" => "postalCode",
            "city" => "city",
            "country" => "country",
            _ => null,
        };
    }
}

internal sealed class CreateClientRequest
{
    public string? LegalName { get; init; }

    public string? DisplayName { get; init; }

    public string? EmailAddress { get; init; }

    public string? PhoneNumber { get; init; }

    public string? AddressLine1 { get; init; }

    public string? AddressLine2 { get; init; }

    public string? PostalCode { get; init; }

    public string? City { get; init; }

    public string? Country { get; init; }

    public string? Status { get; init; }
}

internal sealed class StatusRequest
{
    public string? Status { get; init; }
}

internal sealed class LinkRequest
{
    public string? Login { get; init; }
}
