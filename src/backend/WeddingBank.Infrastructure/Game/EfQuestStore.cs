using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class EfQuestStore(WeddingBankDbContext db) : IQuestStore, IDocumentGate
{
    public async Task<IReadOnlyList<Quest>> ListAsync(CancellationToken cancellationToken)
    {
        return await Ready().ToListAsync(cancellationToken);
    }

    public Task<Quest?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ready().SingleOrDefaultAsync(quest => quest.Id == id, cancellationToken);
    }

    public async Task<Quest?> FindByTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var questId = await db.QuestTasks
            .Where(task => task.Id == taskId)
            .Select(task => (Guid?)task.QuestId)
            .SingleOrDefaultAsync(cancellationToken);
        return questId is Guid id ? await FindAsync(id, cancellationToken) : null;
    }

    public Task<bool> ReferenceInUseAsync(string reference, Guid? exceptId, CancellationToken cancellationToken)
    {
        return exceptId is Guid id
            ? db.Quests.AnyAsync(quest => quest.Reference == reference && quest.Id != id, cancellationToken)
            : db.Quests.AnyAsync(quest => quest.Reference == reference, cancellationToken);
    }

    public Task<bool> TaskReferenceInUseAsync(string reference, Guid? exceptTaskId, CancellationToken cancellationToken)
    {
        return exceptTaskId is Guid id
            ? db.QuestTasks.AnyAsync(task => task.Reference == reference && task.Id != id, cancellationToken)
            : db.QuestTasks.AnyAsync(task => task.Reference == reference, cancellationToken);
    }

    public Task<bool> CaseExistsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return db.CaseFiles.AnyAsync(file => file.Id == caseId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> DocumentsOnCaseAsync(Guid caseId, IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken)
    {
        return await db.CaseDocuments
            .Where(document => document.CaseId == caseId && documentIds.Contains(document.Id))
            .Select(document => document.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, (string Reference, string Title)>> DescribeDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken)
    {
        return await db.CaseDocuments
            .Where(document => documentIds.Contains(document.Id))
            .ToDictionaryAsync(document => document.Id, document => (document.Reference, document.Title), cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> AssignmentsForAsync(IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        return await db.Assignments
            .Where(assignment => assignment.TaskId != null && taskIds.Contains(assignment.TaskId.Value))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        return db.Submissions.CountAsync(submission => submission.AssignmentId == assignmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Submission>> SubmissionsForQuestAsync(Guid questId, CancellationToken cancellationToken)
    {
        var taskIds = await db.QuestTasks.Where(task => task.QuestId == questId).Select(task => task.Id).ToListAsync(cancellationToken);
        var assignmentIds = await db.Assignments
            .Where(assignment => assignment.TaskId != null && taskIds.Contains(assignment.TaskId.Value))
            .Select(assignment => assignment.Id)
            .ToListAsync(cancellationToken);
        var submissions = await db.Submissions
            .Where(submission => assignmentIds.Contains(submission.AssignmentId))
            .ToListAsync(cancellationToken);
        return submissions.OrderByDescending(submission => submission.SubmittedAt).ToArray();
    }

    public void Add(Quest quest)
    {
        db.Quests.Add(quest);
    }

    public void AddAssignment(Assignment assignment)
    {
        db.Assignments.Add(assignment);
    }

    public void RemoveAssignment(Assignment assignment)
    {
        db.Assignments.Remove(assignment);
    }

    public void AddSubmission(Submission submission)
    {
        db.Submissions.Add(submission);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentLocks> LocksForAsync(string login, CancellationToken cancellationToken)
    {
        var rules = await db.DocumentUnlocks
            .AsNoTracking()
            .Select(rule => new { rule.DocumentId, rule.TaskId })
            .ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            return DocumentLocks.None;
        }

        var taskIds = rules.Select(rule => rule.TaskId).Distinct().ToArray();
        var assignments = await db.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.TaskId != null && taskIds.Contains(assignment.TaskId.Value))
            .ToListAsync(cancellationToken);
        var ruled = rules.Select(rule => rule.DocumentId).ToHashSet();
        var hidden = new HashSet<Guid>();
        foreach (var documentId in ruled)
        {
            var unlocked = rules
                .Where(rule => rule.DocumentId == documentId)
                .Select(rule => assignments.SingleOrDefault(assignment => assignment.TaskId == rule.TaskId))
                .Any(assignment => assignment is not null && assignment.CountsFor(login) && assignment.Completed);
            if (!unlocked)
            {
                hidden.Add(documentId);
            }
        }

        return new DocumentLocks(ruled, hidden);
    }

    private IQueryable<Quest> Ready()
    {
        return db.Quests
            .Include(quest => quest.Tasks)
            .ThenInclude(task => task.Answers)
            .Include(quest => quest.Tasks)
            .ThenInclude(task => task.Unlocks);
    }
}
