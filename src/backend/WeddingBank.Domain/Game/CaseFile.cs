namespace WeddingBank.Domain.Game;

public static class Audiences
{
    public const string Juul = "JUUL";
    public const string Amber = "AMBER";
    public const string Both = "BOTH";

    public static bool TryParse(string? value, out string audience)
    {
        audience = value?.Trim().ToUpperInvariant() ?? "";
        if (audience is Juul or Amber or Both)
        {
            return true;
        }

        audience = "";
        return false;
    }

    public static bool Includes(string audience, string login)
    {
        return audience switch
        {
            Both => login is Participants.Juul or Participants.Amber,
            Juul => login == Participants.Juul,
            Amber => login == Participants.Amber,
            _ => false,
        };
    }

    public static bool Fits(string caseAudience, string documentAudience)
    {
        if (caseAudience == Both)
        {
            return documentAudience is Juul or Amber or Both;
        }

        return documentAudience == caseAudience;
    }
}

public static class CaseStatuses
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Archived = "ARCHIVED";

    public static bool TryParse(string? value, out string status)
    {
        status = value?.Trim().ToUpperInvariant() ?? "";
        if (status is Draft or Published or Archived)
        {
            return true;
        }

        status = "";
        return false;
    }
}

public static class DocumentStatuses
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";

    public static bool TryParse(string? value, out string status)
    {
        status = value?.Trim().ToUpperInvariant() ?? "";
        if (status is Draft or Published)
        {
            return true;
        }

        status = "";
        return false;
    }
}

public sealed class CaseFile
{
    private readonly List<CaseDocument> _documents = [];

    private CaseFile()
    {
    }

    public Guid Id { get; private set; }

    public string Reference { get; private set; } = "";

    public string Title { get; private set; } = "";

    public string Summary { get; private set; } = "";

    public string? Introduction { get; private set; }

    public string Status { get; private set; } = CaseStatuses.Draft;

    public string Audience { get; private set; } = Audiences.Both;

    public int DisplayOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<CaseDocument> Documents => _documents;

    public static CaseFile Open(
        Guid id,
        string reference,
        string title,
        string summary,
        string? introduction,
        string audience,
        int displayOrder,
        DateTimeOffset now)
    {
        return new CaseFile
        {
            Id = id,
            Reference = ReferenceText(reference),
            Title = Required(title, 160, "A case needs a title."),
            Summary = Required(summary, 600, "A case needs a summary."),
            Introduction = Optional(introduction, 4000),
            Status = CaseStatuses.Draft,
            Audience = KnownAudience(audience),
            DisplayOrder = displayOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Revise(string reference, string title, string summary, string? introduction, string audience, DateTimeOffset now)
    {
        var nextAudience = KnownAudience(audience);
        if (_documents.Any(document => !Audiences.Fits(nextAudience, document.Audience)))
        {
            throw new InvalidOperationException("A document in this case is addressed more widely than the case.");
        }

        Reference = ReferenceText(reference);
        Title = Required(title, 160, "A case needs a title.");
        Summary = Required(summary, 600, "A case needs a summary.");
        Introduction = Optional(introduction, 4000);
        Audience = nextAudience;
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = CaseStatuses.TryParse(status, out var parsed)
            ? parsed
            : throw new ArgumentException("A case status must be draft, published, or archived.", nameof(status));
        UpdatedAt = now;
    }

    public void Place(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }

    public bool IsOpenTo(string login)
    {
        return Status == CaseStatuses.Published && Audiences.Includes(Audience, login);
    }

    public CaseDocument AddDocument(
        Guid id,
        string reference,
        string title,
        string category,
        string? description,
        string content,
        string audience,
        int displayOrder,
        DateTimeOffset now)
    {
        var nextAudience = KnownAudience(audience);
        if (!Audiences.Fits(Audience, nextAudience))
        {
            throw new InvalidOperationException("A document cannot be shown to someone who cannot open the case.");
        }

        var documentReference = ReferenceText(reference);
        if (_documents.Any(document => document.Reference == documentReference))
        {
            throw new InvalidOperationException("That document reference is already used in this case.");
        }

        var document = CaseDocument.Create(id, Id, documentReference, title, category, description, content, nextAudience, displayOrder, now);
        _documents.Add(document);
        UpdatedAt = now;
        return document;
    }

    public CaseDocument ReviseDocument(
        Guid documentId,
        string reference,
        string title,
        string category,
        string? description,
        string content,
        string audience,
        DateTimeOffset now)
    {
        var document = FindDocument(documentId);
        var nextAudience = KnownAudience(audience);
        if (!Audiences.Fits(Audience, nextAudience))
        {
            throw new InvalidOperationException("A document cannot be shown to someone who cannot open the case.");
        }

        var documentReference = ReferenceText(reference);
        if (_documents.Any(item => item.Id != documentId && item.Reference == documentReference))
        {
            throw new InvalidOperationException("That document reference is already used in this case.");
        }

        document.Revise(documentReference, title, category, description, content, nextAudience, now);
        UpdatedAt = now;
        return document;
    }

    public void SetDocumentStatus(Guid documentId, string status, DateTimeOffset now)
    {
        FindDocument(documentId).SetStatus(status, now);
        UpdatedAt = now;
    }

    public void OrderDocuments(IReadOnlyList<Guid> ids, DateTimeOffset now)
    {
        if (ids.Count != _documents.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => _documents.All(document => document.Id != id)))
        {
            throw new ArgumentException("The document order must list each paper in this case once.", nameof(ids));
        }

        for (var index = 0; index < ids.Count; index++)
        {
            FindDocument(ids[index]).Place(index, now);
        }

        UpdatedAt = now;
    }

    public bool DocumentIsOpenTo(CaseDocument document, string login)
    {
        return IsOpenTo(login) && document.Status == DocumentStatuses.Published && Audiences.Includes(document.Audience, login);
    }

    private CaseDocument FindDocument(Guid documentId)
    {
        return _documents.SingleOrDefault(document => document.Id == documentId)
            ?? throw new InvalidOperationException("That document is not in this case.");
    }

    private static string KnownAudience(string audience)
    {
        return Audiences.TryParse(audience, out var parsed)
            ? parsed
            : throw new ArgumentException("A case is for Juul, Amber, or both.", nameof(audience));
    }

    public static string ReferenceText(string reference)
    {
        var value = reference.Trim().ToUpperInvariant();
        if (value.Length is < 3 or > 40 || value[0] is < 'A' or > 'Z' || value.Any(character => !IsReferenceCharacter(character)))
        {
            throw new ArgumentException("Use a reference such as ARCHIVE-2019-047.");
        }

        return value;
    }

    public static string Required(string value, int max, string message)
    {
        var text = value.Trim();
        if (text.Length is < 1 || text.Length > max)
        {
            throw new ArgumentException(message);
        }

        return text;
    }

    private static bool IsReferenceCharacter(char character)
    {
        return character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-';
    }

    public static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        if (text.Length > max)
        {
            throw new ArgumentException("That text is too long.");
        }

        return text;
    }
}

public sealed class CaseDocument
{
    private CaseDocument()
    {
    }

    public Guid Id { get; private set; }

    public Guid CaseId { get; private set; }

    public string Reference { get; private set; } = "";

    public string Title { get; private set; } = "";

    public string Category { get; private set; } = "";

    public string? Description { get; private set; }

    public string Content { get; private set; } = "";

    public int DisplayOrder { get; private set; }

    public string Status { get; private set; } = DocumentStatuses.Draft;

    public string Audience { get; private set; } = Audiences.Both;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal static CaseDocument Create(
        Guid id,
        Guid caseId,
        string reference,
        string title,
        string category,
        string? description,
        string content,
        string audience,
        int displayOrder,
        DateTimeOffset now)
    {
        return new CaseDocument
        {
            Id = id,
            CaseId = caseId,
            Reference = reference,
            Title = CaseFile.Required(title, 160, "A document needs a title."),
            Category = CaseFile.Required(category, 40, "A document needs a type."),
            Description = CaseFile.Optional(description, 600),
            Content = CaseFile.Required(content, 20000, "A document needs its text."),
            Audience = audience,
            DisplayOrder = displayOrder,
            Status = DocumentStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    internal void Revise(string reference, string title, string category, string? description, string content, string audience, DateTimeOffset now)
    {
        Reference = reference;
        Title = CaseFile.Required(title, 160, "A document needs a title.");
        Category = CaseFile.Required(category, 40, "A document needs a type.");
        Description = CaseFile.Optional(description, 600);
        Content = CaseFile.Required(content, 20000, "A document needs its text.");
        Audience = audience;
        UpdatedAt = now;
    }

    internal void SetStatus(string status, DateTimeOffset now)
    {
        Status = DocumentStatuses.TryParse(status, out var parsed)
            ? parsed
            : throw new ArgumentException("A document is either a draft or published.", nameof(status));
        UpdatedAt = now;
    }

    internal void Place(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }
}
