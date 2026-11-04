using WeddingBank.Domain.Game;

namespace WeddingBank.Application.Game;

public sealed class CaseService(ICaseStore cases, TimeProvider clock)
{
    public async Task<IReadOnlyList<CaseSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var files = await cases.ListAsync(cancellationToken);
        return files.OrderBy(file => file.DisplayOrder).ThenBy(file => file.Reference).Select(SummaryOf).ToArray();
    }

    public async Task<OperationResult<CaseDetail>> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(id, cancellationToken);
        return file is null
            ? OperationResult<CaseDetail>.NotFound("That case could not be found.")
            : OperationResult<CaseDetail>.Ok(DetailOf(file));
    }

    public async Task<OperationResult<CaseDetail>> CreateAsync(CaseDraft draft, CancellationToken cancellationToken)
    {
        if (!TryCase(draft, out var reference, out var title, out var summary, out var introduction, out var audience, out var errors))
        {
            return OperationResult<CaseDetail>.Invalid(errors);
        }

        if (await cases.ReferenceInUseAsync(reference, null, cancellationToken))
        {
            return OperationResult<CaseDetail>.Conflict("That case reference is already in use.");
        }

        var files = await cases.ListAsync(cancellationToken);
        var order = files.Count == 0 ? 0 : files.Max(file => file.DisplayOrder) + 1;
        var file = CaseFile.Open(Guid.CreateVersion7(), reference, title, summary, introduction, audience, order, clock.GetUtcNow());
        cases.Add(file);
        await cases.SaveAsync(cancellationToken);
        return OperationResult<CaseDetail>.Ok(DetailOf(file));
    }

    public async Task<OperationResult<CaseDetail>> UpdateAsync(Guid id, CaseDraft draft, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(id, cancellationToken);
        if (file is null)
        {
            return OperationResult<CaseDetail>.NotFound("That case could not be found.");
        }

        if (!TryCase(draft, out var reference, out var title, out var summary, out var introduction, out var audience, out var errors))
        {
            return OperationResult<CaseDetail>.Invalid(errors);
        }

        if (await cases.ReferenceInUseAsync(reference, id, cancellationToken))
        {
            return OperationResult<CaseDetail>.Conflict("That case reference is already in use.");
        }

        try
        {
            file.Revise(reference, title, summary, introduction, audience, clock.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return OperationResult<CaseDetail>.Invalid(Field("audience", "Narrow the documents before narrowing who can open this case."));
        }

        await cases.SaveAsync(cancellationToken);
        return OperationResult<CaseDetail>.Ok(DetailOf(file));
    }

    public async Task<OperationResult<CaseDetail>> SetStatusAsync(Guid id, string? status, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(id, cancellationToken);
        if (file is null)
        {
            return OperationResult<CaseDetail>.NotFound("That case could not be found.");
        }

        if (!CaseStatuses.TryParse(status, out var parsed))
        {
            return OperationResult<CaseDetail>.Invalid(Field("status", "Choose draft, published, or archived."));
        }

        file.SetStatus(parsed, clock.GetUtcNow());
        await cases.SaveAsync(cancellationToken);
        return OperationResult<CaseDetail>.Ok(DetailOf(file));
    }

    public async Task<OperationResult<IReadOnlyList<CaseSummary>>> ReorderAsync(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken)
    {
        var files = await cases.ListAsync(cancellationToken);
        if (ids is null || ids.Count != files.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => files.All(file => file.Id != id)))
        {
            return OperationResult<IReadOnlyList<CaseSummary>>.Invalid(Field("ids", "The order must list each case once."));
        }

        var now = clock.GetUtcNow();
        for (var index = 0; index < ids.Count; index++)
        {
            files.Single(file => file.Id == ids[index]).Place(index, now);
        }

        await cases.SaveAsync(cancellationToken);
        return OperationResult<IReadOnlyList<CaseSummary>>.Ok(files.OrderBy(file => file.DisplayOrder).ThenBy(file => file.Reference).Select(SummaryOf).ToArray());
    }

    public async Task<OperationResult<DocumentDetail>> AddDocumentAsync(Guid caseId, DocumentDraft draft, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(caseId, cancellationToken);
        if (file is null)
        {
            return OperationResult<DocumentDetail>.NotFound("That case could not be found.");
        }

        if (!TryDocument(draft, out var reference, out var title, out var category, out var description, out var content, out var audience, out var errors))
        {
            return OperationResult<DocumentDetail>.Invalid(errors);
        }

        try
        {
            var order = file.Documents.Count == 0 ? 0 : file.Documents.Max(document => document.DisplayOrder) + 1;
            var document = file.AddDocument(Guid.CreateVersion7(), reference, title, category, description, content, audience, order, clock.GetUtcNow());
            await cases.SaveAsync(cancellationToken);
            return OperationResult<DocumentDetail>.Ok(DetailOf(document));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("reference", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<DocumentDetail>.Conflict("That document reference is already used in this case.");
        }
        catch (InvalidOperationException)
        {
            return OperationResult<DocumentDetail>.Invalid(Field("audience", "A document cannot reach someone who cannot open the case."));
        }
    }

    public async Task<OperationResult<DocumentDetail>> ReadDocumentAsync(Guid caseId, Guid documentId, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(caseId, cancellationToken);
        var document = file?.Documents.SingleOrDefault(item => item.Id == documentId);
        return document is null
            ? OperationResult<DocumentDetail>.NotFound("That document could not be found.")
            : OperationResult<DocumentDetail>.Ok(DetailOf(document));
    }

    public async Task<OperationResult<DocumentDetail>> UpdateDocumentAsync(Guid caseId, Guid documentId, DocumentDraft draft, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(caseId, cancellationToken);
        if (file is null || file.Documents.All(document => document.Id != documentId))
        {
            return OperationResult<DocumentDetail>.NotFound("That document could not be found.");
        }

        if (!TryDocument(draft, out var reference, out var title, out var category, out var description, out var content, out var audience, out var errors))
        {
            return OperationResult<DocumentDetail>.Invalid(errors);
        }

        try
        {
            var document = file.ReviseDocument(documentId, reference, title, category, description, content, audience, clock.GetUtcNow());
            await cases.SaveAsync(cancellationToken);
            return OperationResult<DocumentDetail>.Ok(DetailOf(document));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("reference", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<DocumentDetail>.Conflict("That document reference is already used in this case.");
        }
        catch (InvalidOperationException)
        {
            return OperationResult<DocumentDetail>.Invalid(Field("audience", "A document cannot reach someone who cannot open the case."));
        }
    }

    public async Task<OperationResult<DocumentDetail>> SetDocumentStatusAsync(Guid caseId, Guid documentId, string? status, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(caseId, cancellationToken);
        if (file is null || file.Documents.All(document => document.Id != documentId))
        {
            return OperationResult<DocumentDetail>.NotFound("That document could not be found.");
        }

        if (!DocumentStatuses.TryParse(status, out var parsed))
        {
            return OperationResult<DocumentDetail>.Invalid(Field("status", "A document is either a draft or published."));
        }

        file.SetDocumentStatus(documentId, parsed, clock.GetUtcNow());
        await cases.SaveAsync(cancellationToken);
        return OperationResult<DocumentDetail>.Ok(DetailOf(file.Documents.Single(document => document.Id == documentId)));
    }

    public async Task<OperationResult<CaseDetail>> ReorderDocumentsAsync(Guid caseId, IReadOnlyList<Guid>? ids, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(caseId, cancellationToken);
        if (file is null)
        {
            return OperationResult<CaseDetail>.NotFound("That case could not be found.");
        }

        try
        {
            file.OrderDocuments(ids ?? [], clock.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return OperationResult<CaseDetail>.Invalid(Field("ids", "The order must list each document in this case once."));
        }

        await cases.SaveAsync(cancellationToken);
        return OperationResult<CaseDetail>.Ok(DetailOf(file));
    }

    public async Task<IReadOnlyList<ReadingCase>> ListForAsync(string login, CancellationToken cancellationToken)
    {
        var files = await cases.ListAsync(cancellationToken);
        return files
            .Where(file => file.IsOpenTo(login))
            .OrderBy(file => file.DisplayOrder)
            .ThenBy(file => file.Reference)
            .Select(file => ReadingOf(file, login, includeDocuments: false))
            .ToArray();
    }

    public async Task<OperationResult<ReadingCase>> ReadForAsync(string login, Guid id, CancellationToken cancellationToken)
    {
        var file = await cases.FindAsync(id, cancellationToken);
        if (file is null || !file.IsOpenTo(login))
        {
            return OperationResult<ReadingCase>.NotFound("That case is not available.");
        }

        return OperationResult<ReadingCase>.Ok(ReadingOf(file, login, includeDocuments: true));
    }

    public async Task<IReadOnlyList<ReadingDocument>> PapersForAsync(string login, CancellationToken cancellationToken)
    {
        var files = await cases.ListAsync(cancellationToken);
        return files
            .Where(file => file.IsOpenTo(login))
            .OrderBy(file => file.DisplayOrder)
            .ThenBy(file => file.Reference)
            .SelectMany(file => OpenDocuments(file, login).Select(document => PaperOf(file, document)))
            .ToArray();
    }

    public async Task<OperationResult<ReadingPaper>> ReadPaperAsync(string login, Guid documentId, CancellationToken cancellationToken)
    {
        var files = await cases.ListAsync(cancellationToken);
        foreach (var file in files)
        {
            var document = file.Documents.SingleOrDefault(item => item.Id == documentId);
            if (document is null)
            {
                continue;
            }

            return file.DocumentIsOpenTo(document, login)
                ? OperationResult<ReadingPaper>.Ok(PaperTextOf(file, document))
                : OperationResult<ReadingPaper>.NotFound("That document is not available.");
        }

        return OperationResult<ReadingPaper>.NotFound("That document is not available.");
    }

    private static bool TryCase(
        CaseDraft draft,
        out string reference,
        out string title,
        out string summary,
        out string? introduction,
        out string audience,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        reference = Read("reference", () => CaseFile.ReferenceText(draft.Reference ?? ""), errors);
        title = Read("title", () => CaseFile.Required(draft.Title ?? "", 160, "A case needs a title."), errors);
        summary = Read("summary", () => CaseFile.Required(draft.Summary ?? "", 600, "A case needs a summary."), errors);
        introduction = ReadOptional("introduction", () => CaseFile.Optional(draft.Introduction, 4000), errors);
        audience = "";
        if (!Audiences.TryParse(draft.Audience, out audience))
        {
            errors["audience"] = ["Choose Juul, Amber, or both."];
        }

        return errors.Count == 0;
    }

    private static bool TryDocument(
        DocumentDraft draft,
        out string reference,
        out string title,
        out string category,
        out string? description,
        out string content,
        out string audience,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        reference = Read("reference", () => CaseFile.ReferenceText(draft.Reference ?? ""), errors);
        title = Read("title", () => CaseFile.Required(draft.Title ?? "", 160, "A document needs a title."), errors);
        category = Read("category", () => CaseFile.Required(draft.Category ?? "", 40, "A document needs a type."), errors);
        description = ReadOptional("description", () => CaseFile.Optional(draft.Description, 600), errors);
        content = Read("content", () => CaseFile.Required(draft.Content ?? "", 20000, "A document needs its text."), errors);
        audience = "";
        if (!Audiences.TryParse(draft.Audience, out audience))
        {
            errors["audience"] = ["Choose Juul, Amber, or both."];
        }

        return errors.Count == 0;
    }

    private static string Read(string key, Func<string> read, Dictionary<string, string[]> errors)
    {
        try
        {
            return read();
        }
        catch (ArgumentException exception)
        {
            errors[key] = [exception.Message];
            return "";
        }
    }

    private static string? ReadOptional(string key, Func<string?> read, Dictionary<string, string[]> errors)
    {
        try
        {
            return read();
        }
        catch (ArgumentException exception)
        {
            errors[key] = [exception.Message];
            return null;
        }
    }

    private static Dictionary<string, string[]> Field(string key, string message)
    {
        return new Dictionary<string, string[]> { [key] = [message] };
    }

    private static CaseSummary SummaryOf(CaseFile file)
    {
        return new CaseSummary(file.Id, file.Reference, file.Title, file.Summary, file.Status, file.Audience, file.DisplayOrder, file.Documents.Count, file.UpdatedAt);
    }

    private static CaseDetail DetailOf(CaseFile file)
    {
        var documents = file.Documents
            .OrderBy(document => document.DisplayOrder)
            .ThenBy(document => document.Reference)
            .Select(document => new DocumentSummary(document.Id, document.Reference, document.Title, document.Category, document.Description, document.Status, document.Audience, document.DisplayOrder, document.UpdatedAt))
            .ToArray();
        return new CaseDetail(file.Id, file.Reference, file.Title, file.Summary, file.Introduction, file.Status, file.Audience, file.DisplayOrder, file.CreatedAt, file.UpdatedAt, documents);
    }

    private static DocumentDetail DetailOf(CaseDocument document)
    {
        return new DocumentDetail(document.Id, document.CaseId, document.Reference, document.Title, document.Category, document.Description, document.Content, document.Status, document.Audience, document.DisplayOrder, document.CreatedAt, document.UpdatedAt);
    }

    private static ReadingCase ReadingOf(CaseFile file, string login, bool includeDocuments)
    {
        IReadOnlyList<ReadingDocument> documents = includeDocuments
            ? OpenDocuments(file, login).Select(document => PaperOf(file, document)).ToArray()
            : [];
        return new ReadingCase(file.Id, file.Reference, file.Title, file.Summary, file.Introduction, documents);
    }

    private static IEnumerable<CaseDocument> OpenDocuments(CaseFile file, string login)
    {
        return file.Documents
            .Where(document => file.DocumentIsOpenTo(document, login))
            .OrderBy(document => document.DisplayOrder)
            .ThenBy(document => document.Reference);
    }

    private static ReadingDocument PaperOf(CaseFile file, CaseDocument document)
    {
        return new ReadingDocument(document.Id, file.Id, file.Reference, file.Title, document.Reference, document.Title, document.Category, document.Description, document.DisplayOrder);
    }

    private static ReadingPaper PaperTextOf(CaseFile file, CaseDocument document)
    {
        return new ReadingPaper(document.Id, file.Id, file.Reference, file.Title, document.Reference, document.Title, document.Category, document.Description, document.Content, document.DisplayOrder);
    }
}
