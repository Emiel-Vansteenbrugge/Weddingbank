using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class EfGameStore(WeddingBankDbContext db) : IGameStore
{
    public async Task<ProgressCounts> ReadCountsAsync(CancellationToken cancellationToken)
    {
        var assignments = await db.Assignments.AsNoTracking().ToListAsync(cancellationToken);
        var notices = await db.Notices.AsNoTracking().ToListAsync(cancellationToken);
        return new ProgressCounts(
            Count(assignments, ProgressScope.Individual, Participants.Juul, completed: true),
            Count(assignments, ProgressScope.Individual, Participants.Juul, completed: false),
            Count(assignments, ProgressScope.Individual, Participants.Amber, completed: true),
            Count(assignments, ProgressScope.Individual, Participants.Amber, completed: false),
            Count(assignments, ProgressScope.Shared, null, completed: true),
            Count(assignments, ProgressScope.Shared, null, completed: false),
            notices.Count(notice => notice.Kind == "HINT" && notice.ParticipantLogin == Participants.Juul),
            notices.Count(notice => notice.Kind == "HINT" && notice.ParticipantLogin == Participants.Amber),
            notices.Count(notice => notice.Kind == "HINT" && notice.ParticipantLogin is null),
            await db.PhoneCalls.CountAsync(call => !call.Handled, cancellationToken),
            await db.Submissions.CountAsync(submission => submission.Outcome == "PENDING", cancellationToken),
            await LastActivityAsync(Participants.Juul, cancellationToken),
            await LastActivityAsync(Participants.Amber, cancellationToken));
    }

    private static int Count(IEnumerable<Assignment> assignments, ProgressScope scope, string? login, bool completed)
    {
        return assignments.Count(item =>
            item.Scope == scope && item.ParticipantLogin == login && item.Completed == completed);
    }

    private async Task<DateTimeOffset?> LastActivityAsync(string login, CancellationToken cancellationToken)
    {
        var stamps = await db.Activities
            .AsNoTracking()
            .Where(activity => activity.ParticipantLogin == login)
            .Select(activity => activity.OccurredAt)
            .ToListAsync(cancellationToken);
        return stamps.Count == 0 ? null : stamps.Max();
    }

    public async Task<VisibleProgress> ReadVisibleProgressAsync(string login, CancellationToken cancellationToken)
    {
        var rows = await db.Assignments.AsNoTracking()
            .Where(assignment => assignment.TaskId != null)
            .Join(
                db.QuestTasks.AsNoTracking(),
                assignment => assignment.TaskId,
                task => task.Id,
                (assignment, task) => new { assignment, task })
            .Join(
                db.Quests.AsNoTracking(),
                row => row.task.QuestId,
                quest => quest.Id,
                (row, quest) => new { row.assignment, row.task, quest })
            .Join(
                db.CaseFiles.AsNoTracking(),
                row => row.quest.CaseId,
                file => file.Id,
                (row, file) => new { row.assignment, row.task, row.quest, file })
            .ToListAsync(cancellationToken);
        var assignments = rows.Select(row => row.assignment).ToArray();
        var visible = rows
            .Where(row => row.quest.IsOpenTo(login) && row.quest.IsReleased(assignments, login) && row.file.IsOpenTo(login) && row.task.IsVisibleTo(login, row.assignment))
            .Select(row => row.assignment)
            .ToArray();
        return new VisibleProgress(
            visible.Count(item => item.Scope == ProgressScope.Individual && item.ParticipantLogin == login && item.Completed),
            visible.Count(item => item.Scope == ProgressScope.Individual && item.ParticipantLogin == login && !item.Completed),
            visible.Count(item => item.Scope == ProgressScope.Shared && item.Completed),
            visible.Count(item => item.Scope == ProgressScope.Shared && !item.Completed));
    }
}
