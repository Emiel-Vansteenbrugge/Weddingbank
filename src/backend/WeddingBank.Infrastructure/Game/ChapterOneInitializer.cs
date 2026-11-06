using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class ChapterOneReport
{
    public List<string> Lines { get; } = [];

    public List<string> Conflicts { get; } = [];
}

public sealed class ChapterOneInitializer(WeddingBankDbContext db, TimeProvider clock)
{
    public async Task<ChapterOneReport> EnsureAsync(CancellationToken cancellationToken = default)
    {
        var report = new ChapterOneReport();
        var now = clock.GetUtcNow();
        if (await HasMisplacedDocumentsAsync(report, cancellationToken))
        {
            return report;
        }

        var tasks = new Dictionary<string, QuestTask>(StringComparer.Ordinal);
        foreach (var spec in ChapterOneCatalog.Cases)
        {
            var file = await EnsureCaseAsync(spec, now, report, cancellationToken);
            for (var index = 0; index < spec.Documents.Length; index++)
            {
                EnsureDocument(file, spec.Documents[index], index, now, report);
            }

            foreach (var quest in spec.Quests)
            {
                if (await EnsureQuestAsync(file, quest, tasks, now, report, cancellationToken) is false)
                {
                    return Conflict(report, null);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return report;
    }

    private async Task<bool> HasMisplacedDocumentsAsync(ChapterOneReport report, CancellationToken cancellationToken)
    {
        var misplaced = new List<string>();
        foreach (var spec in ChapterOneCatalog.Cases)
        {
            var references = spec.Documents.Select(document => document.Reference).ToArray();
            var owner = await db.CaseFiles.AsNoTracking().SingleOrDefaultAsync(item => item.Reference == spec.Reference, cancellationToken);
            var query = db.CaseDocuments.Where(document => references.Contains(document.Reference));
            if (owner is not null)
            {
                var caseId = owner.Id;
                query = query.Where(document => document.CaseId != caseId);
            }

            misplaced.AddRange(await query.Select(document => document.Reference).ToListAsync(cancellationToken));
        }

        if (misplaced.Count == 0)
        {
            return false;
        }

        report.Conflicts.Add($"{string.Join(", ", misplaced.Distinct().Order())} already belongs to another case.");
        return true;
    }

    private async Task<CaseFile> EnsureCaseAsync(ChapterOneCase spec, DateTimeOffset now, ChapterOneReport report, CancellationToken cancellationToken)
    {
        var file = await db.CaseFiles
            .Include(item => item.Documents)
            .SingleOrDefaultAsync(item => item.Reference == spec.Reference, cancellationToken);
        if (file is null)
        {
            var stored = await db.CaseFiles.Select(item => (int?)item.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
            var pending = db.ChangeTracker.Entries<CaseFile>().Select(entry => entry.Entity.DisplayOrder).DefaultIfEmpty(-1).Max();
            file = CaseFile.Open(Guid.CreateVersion7(), spec.Reference, spec.Title, spec.Summary, spec.Introduction, Audiences.Both, Math.Max(stored, pending) + 1, now);
            db.CaseFiles.Add(file);
            report.Lines.Add($"created case {spec.Reference}");
        }
        else if (ChangedCase(file, spec))
        {
            file.Revise(spec.Reference, spec.Title, spec.Summary, spec.Introduction, Audiences.Both, now);
            report.Lines.Add($"repaired case {spec.Reference}");
        }
        else
        {
            report.Lines.Add($"unchanged case {spec.Reference}");
        }

        if (file.Status != CaseStatuses.Published)
        {
            file.SetStatus(CaseStatuses.Published, now);
            report.Lines.Add($"published case {spec.Reference}");
        }

        return file;
    }

    private async Task<bool> EnsureQuestAsync(
        CaseFile file,
        ChapterOneQuest spec,
        Dictionary<string, QuestTask> tasks,
        DateTimeOffset now,
        ChapterOneReport report,
        CancellationToken cancellationToken)
    {
        var quest = await db.Quests
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Answers)
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Unlocks)
            .SingleOrDefaultAsync(item => item.Reference == spec.Reference, cancellationToken);
        if (quest is not null && quest.CaseId != file.Id)
        {
            report.Conflicts.Add($"{spec.Reference} is attached to a different case.");
            return false;
        }

        var taskQuery = db.QuestTasks.Where(item => item.Reference == spec.Task.Reference);
        if (quest is not null)
        {
            var questId = quest.Id;
            taskQuery = taskQuery.Where(item => item.QuestId != questId);
        }

        if (await taskQuery.AnyAsync(cancellationToken))
        {
            report.Conflicts.Add($"{spec.Task.Reference} already belongs to another quest.");
            return false;
        }

        if (quest is null)
        {
            var stored = await db.Quests.Select(item => (int?)item.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
            var pending = db.ChangeTracker.Entries<Quest>().Select(entry => entry.Entity.DisplayOrder).DefaultIfEmpty(-1).Max();
            quest = Quest.Open(Guid.CreateVersion7(), spec.Reference, spec.Title, spec.Description, spec.Instructions, file.Id, Audiences.Both, spec.Kind, Math.Max(stored, pending) + 1, now);
            db.Quests.Add(quest);
            report.Lines.Add($"created quest {spec.Reference}");
        }
        else if (ChangedQuest(quest, spec))
        {
            quest.Revise(spec.Reference, spec.Title, spec.Description, spec.Instructions, file.Id, Audiences.Both, spec.Kind, now);
            report.Lines.Add($"repaired quest {spec.Reference}");
        }
        else
        {
            report.Lines.Add($"unchanged quest {spec.Reference}");
        }

        if (spec.Published && quest.Status != CaseStatuses.Published)
        {
            quest.SetStatus(CaseStatuses.Published, now);
            report.Lines.Add($"published quest {spec.Reference}");
        }

        var task = quest.Tasks.SingleOrDefault(item => item.Reference == spec.Task.Reference);
        if (task is null)
        {
            var taskOrder = quest.Tasks.Count == 0 ? 0 : quest.Tasks.Max(item => item.DisplayOrder) + 1;
            task = quest.AddTask(Guid.CreateVersion7(), spec.Task.Reference, spec.Task.Title, spec.Task.Instructions, TaskTypes.Text, ProgressScope.Shared, null, true, true, spec.Task.SuccessMessage, taskOrder, now);
            report.Lines.Add($"created task {spec.Task.Reference}");
        }
        else if (await RepairTaskAsync(quest, task, spec.Task, now, report, cancellationToken) is false)
        {
            return false;
        }

        if (spec.Published && task.Status != TaskStatuses.Published)
        {
            quest.SetTaskStatus(task.Id, TaskStatuses.Published, now);
            report.Lines.Add($"published task {spec.Task.Reference}");
        }

        tasks[task.Reference] = task;
        if (spec.ReleasedAfterTask is string prior)
        {
            var priorTask = tasks[prior];
            if (quest.ReleasedByTaskId != priorTask.Id)
            {
                quest.HoldUntil(priorTask.Id, now);
                report.Lines.Add($"held quest {spec.Reference} until {prior}");
            }
            else
            {
                report.Lines.Add($"unchanged release {spec.Reference}");
            }
        }

        EnsureAnswer(task, spec.Task, report);
        var unlocks = spec.Task.Unlocks
            .Select(reference => (reference, file.Documents.Single(document => document.Reference == reference).Id))
            .ToArray();
        EnsureUnlocks(task, unlocks, report);
        return await EnsureAssignmentAsync(task, now, report, cancellationToken);
    }

    private static void EnsureDocument(CaseFile file, ChapterOneDocument spec, int order, DateTimeOffset now, ChapterOneReport report)
    {
        var document = file.Documents.SingleOrDefault(item => item.Reference == spec.Reference);
        var wanted = spec.Published ? DocumentStatuses.Published : DocumentStatuses.Draft;
        if (document is null)
        {
            document = file.AddDocument(Guid.CreateVersion7(), spec.Reference, spec.Title, spec.Category, null, spec.Content, spec.Audience, order, now);
            report.Lines.Add($"created document {spec.Reference}");
        }
        else if (ChangedDocument(document, spec))
        {
            file.ReviseDocument(document.Id, spec.Reference, spec.Title, spec.Category, document.Description, spec.Content, spec.Audience, now);
            report.Lines.Add($"repaired document {spec.Reference}");
        }
        else
        {
            report.Lines.Add($"unchanged document {spec.Reference}");
        }

        if (document.Status != wanted)
        {
            file.SetDocumentStatus(document.Id, wanted, now);
            report.Lines.Add($"{(spec.Published ? "published" : "held")} document {spec.Reference}");
        }
    }

    private async Task<bool> RepairTaskAsync(Quest quest, QuestTask task, ChapterOneTask spec, DateTimeOffset now, ChapterOneReport report, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(item => item.TaskId == task.Id, cancellationToken);
        var scopeChanged = task.Scope != ProgressScope.Shared || task.ParticipantLogin is not null;
        if (scopeChanged && assignment is not null && (assignment.Completed || await db.Submissions.AnyAsync(item => item.AssignmentId == assignment.Id, cancellationToken)))
        {
            report.Conflicts.Add($"{spec.Reference} already has progress, so its shared assignment cannot be repaired.");
            return false;
        }

        if (ChangedTask(task, spec) || scopeChanged)
        {
            quest.ReviseTask(task.Id, spec.Reference, spec.Title, spec.Instructions, TaskTypes.Text, ProgressScope.Shared, null, true, true, spec.SuccessMessage, now);
            report.Lines.Add($"repaired task {spec.Reference}");
        }
        else
        {
            report.Lines.Add($"unchanged task {spec.Reference}");
        }

        return true;
    }

    private static void EnsureAnswer(QuestTask task, ChapterOneTask spec, ChapterOneReport report)
    {
        var answers = task.Answers.OrderBy(answer => answer.DisplayOrder).Select(answer => answer.Text).ToArray();
        if (answers.Length == 1 && answers[0] == spec.AcceptedAnswer)
        {
            report.Lines.Add($"unchanged answer {spec.Reference}");
            return;
        }

        task.ReplaceAnswers([(Guid.CreateVersion7(), spec.AcceptedAnswer)]);
        report.Lines.Add($"{(answers.Length == 0 ? "recorded" : "repaired")} answer {spec.Reference}");
    }

    private static void EnsureUnlocks(QuestTask task, IReadOnlyList<(string Reference, Guid DocumentId)> wanted, ChapterOneReport report)
    {
        var current = task.Unlocks.Select(unlock => unlock.DocumentId).ToHashSet();
        var wantedIds = wanted.Select(item => item.DocumentId).ToHashSet();
        if (!current.SetEquals(wantedIds))
        {
            var kept = task.Unlocks.Where(unlock => wantedIds.Contains(unlock.DocumentId)).Select(unlock => (unlock.Id, unlock.DocumentId));
            var added = wanted.Where(item => !current.Contains(item.DocumentId)).Select(item => (Guid.CreateVersion7(), item.DocumentId));
            task.ReplaceUnlocks(kept.Concat(added).ToArray());
        }

        foreach (var item in wanted)
        {
            report.Lines.Add($"{(current.Contains(item.DocumentId) ? "unchanged" : "linked")} unlock {item.Reference}");
        }
    }

    private async Task<bool> EnsureAssignmentAsync(QuestTask task, DateTimeOffset now, ChapterOneReport report, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(item => item.TaskId == task.Id, cancellationToken);
        if (assignment is null)
        {
            db.Assignments.Add(Assignment.ForTheHouse(Guid.CreateVersion7(), task.Reference, now, task.Id));
            report.Lines.Add($"created shared assignment {task.Reference}");
            return true;
        }

        if (assignment.Scope != ProgressScope.Shared || assignment.ParticipantLogin is not null)
        {
            if (assignment.Completed || await db.Submissions.AnyAsync(item => item.AssignmentId == assignment.Id, cancellationToken))
            {
                report.Conflicts.Add($"{task.Reference} already has progress, so its shared assignment cannot be repaired.");
                return false;
            }

            db.Assignments.Remove(assignment);
            db.Assignments.Add(Assignment.ForTheHouse(Guid.CreateVersion7(), task.Reference, now, task.Id));
            report.Lines.Add($"repaired shared assignment {task.Reference}");
            return true;
        }

        if (!string.Equals(assignment.Code, task.Reference, StringComparison.Ordinal))
        {
            assignment.Rename(task.Reference, now);
            report.Lines.Add($"repaired shared assignment {task.Reference}");
        }
        else
        {
            report.Lines.Add(assignment.Completed
                ? $"kept completed assignment {task.Reference}"
                : $"unchanged shared assignment {task.Reference}");
        }

        return true;
    }

    private static bool ChangedCase(CaseFile file, ChapterOneCase spec)
    {
        return file.Title != spec.Title
            || file.Summary != spec.Summary
            || (file.Introduction ?? "") != spec.Introduction
            || file.Audience != Audiences.Both;
    }

    private static bool ChangedQuest(Quest quest, ChapterOneQuest spec)
    {
        return quest.Title != spec.Title
            || quest.Description != spec.Description
            || quest.Instructions != spec.Instructions
            || quest.Audience != Audiences.Both
            || quest.Kind != spec.Kind;
    }

    private static bool ChangedTask(QuestTask task, ChapterOneTask spec)
    {
        return task.Title != spec.Title
            || task.Instructions != spec.Instructions
            || task.TaskType != TaskTypes.Text
            || !task.IgnoreCase
            || !task.TrimWhitespace
            || task.SuccessMessage != spec.SuccessMessage;
    }

    private ChapterOneReport Conflict(ChapterOneReport report, string? conflict)
    {
        if (conflict is not null)
        {
            report.Conflicts.Add(conflict);
        }

        report.Lines.Clear();
        db.ChangeTracker.Clear();
        return report;
    }

    private static bool ChangedDocument(CaseDocument document, ChapterOneDocument spec)
    {
        return document.Title != spec.Title
            || document.Category != spec.Category
            || document.Content != spec.Content
            || document.Audience != spec.Audience;
    }
}
