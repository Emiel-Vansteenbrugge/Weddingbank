using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class ChapterOneInitializerTests
{
    [Fact]
    public async Task Chapter_I_is_created_once_and_completed_progress_stays()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

        await using (var db = new WeddingBankDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var first = await new ChapterOneInitializer(db, new FixedTime(now)).EnsureAsync();
            Assert.Empty(first.Conflicts);
            Assert.Contains(first.Lines, line => line == "created case CH-I-001");
            Assert.Contains(first.Lines, line => line == "created quest CH-I-QUEST-001");
            Assert.Contains(first.Lines, line => line == "created quest CH-I-QUEST-002");
            Assert.Contains(first.Lines, line => line == "recorded answer CH-I-TASK-001");
            Assert.Contains(first.Lines, line => line == "linked unlock CH-I-DOC-007");
            Assert.Contains(first.Lines, line => line == "held quest CH-I-QUEST-002 until CH-I-TASK-001");
        }

        Guid caseId;
        Guid questId;
        Guid assignmentId;
        Guid submissionId;
        await using (var db = new WeddingBankDbContext(options))
        {
            Assert.Equal(1, await db.CaseFiles.CountAsync(file => file.Reference == "CH-I-001"));
            Assert.Equal(23, await db.CaseDocuments.CountAsync());
            Assert.Equal(18, await db.CaseDocuments.CountAsync(document => document.Status == DocumentStatuses.Published));
            var locked = await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-007");
            Assert.Equal(DocumentStatuses.Draft, locked.Status);
            Assert.Equal(DocumentStatuses.Draft, (await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-011")).Status);
            Assert.DoesNotContain("29 October 2026", (await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-003")).Content, StringComparison.Ordinal);
            Assert.Equal(Audiences.Juul, (await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-004")).Audience);
            Assert.Equal(Audiences.Amber, (await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-005")).Audience);
            var quest = await db.Quests.Include(item => item.Tasks).ThenInclude(task => task.Answers).SingleAsync(item => item.Reference == "CH-I-QUEST-001");
            var task = quest.Tasks.Single();
            var second = await db.Quests.Include(item => item.Tasks).SingleAsync(item => item.Reference == "CH-I-QUEST-002");
            Assert.Equal(task.Id, second.ReleasedByTaskId);
            Assert.Equal(CaseStatuses.Published, quest.Status);
            Assert.Equal(Audiences.Both, quest.Audience);
            Assert.Equal(QuestKinds.Main, quest.Kind);
            Assert.Equal(TaskStatuses.Published, task.Status);
            Assert.Equal(ProgressScope.Shared, task.Scope);
            Assert.Equal(["REG-1026-17"], task.Answers.Select(answer => answer.Text).ToArray());
            var assignment = await db.Assignments.SingleAsync(item => item.TaskId == task.Id);
            Assert.Equal(ProgressScope.Shared, assignment.Scope);
            Assert.False(assignment.Completed);
            assignment.Complete(now);
            var later = await db.Assignments.SingleAsync(item => item.TaskId == second.Tasks.Single().Id);
            later.Complete(now);
            submissionId = Guid.CreateVersion7();
            db.Submissions.Add(Submission.Decide(submissionId, assignment.Id, Participants.Juul, "REG-1026-14", false, now));
            task.ReplaceAnswers([(Guid.CreateVersion7(), "WRONG")]);
            var file = await db.CaseFiles.Include(item => item.Documents).SingleAsync(item => item.Reference == "CH-I-001");
            file.SetDocumentStatus(locked.Id, DocumentStatuses.Published, now);
            await db.SaveChangesAsync();
            caseId = file.Id;
            questId = quest.Id;
            assignmentId = assignment.Id;
        }

        await using (var db = new WeddingBankDbContext(options))
        {
            var second = await new ChapterOneInitializer(db, new FixedTime(now.AddHours(1))).EnsureAsync();
            Assert.Empty(second.Conflicts);
            Assert.DoesNotContain(second.Lines, line => line.StartsWith("created ", StringComparison.Ordinal));
            Assert.Contains(second.Lines, line => line == "repaired answer CH-I-TASK-001");
            Assert.Contains(second.Lines, line => line == "held document CH-I-DOC-007");
            Assert.Contains(second.Lines, line => line == "kept completed assignment CH-I-TASK-001");
            Assert.Contains(second.Lines, line => line == "kept completed assignment CH-I-TASK-002");
            Assert.Contains(second.Lines, line => line == "unchanged release CH-I-QUEST-002");
        }

        await using (var db = new WeddingBankDbContext(options))
        {
            Assert.Equal(caseId, (await db.CaseFiles.SingleAsync(item => item.Reference == "CH-I-001")).Id);
            Assert.Equal(questId, (await db.Quests.SingleAsync(item => item.Reference == "CH-I-QUEST-001")).Id);
            Assert.Equal(5, await db.Quests.CountAsync());
            Assert.Equal(5, await db.QuestTasks.CountAsync());
            Assert.Equal(5, await db.Assignments.CountAsync());
            Assert.Equal(11, await db.DocumentUnlocks.CountAsync());
            var assignment = await db.Assignments.SingleAsync(item => item.Id == assignmentId);
            Assert.True(assignment.Completed);
            var secondTaskId = (await db.QuestTasks.SingleAsync(item => item.Reference == "CH-I-TASK-002")).Id;
            Assert.True((await db.Assignments.SingleAsync(item => item.TaskId == secondTaskId)).Completed);
            Assert.Equal(2, await db.Assignments.CountAsync(item => item.Completed));
            Assert.Equal(submissionId, (await db.Submissions.SingleAsync()).Id);
            var taskId = (await db.QuestTasks.SingleAsync(item => item.Reference == "CH-I-TASK-001")).Id;
            var answer = await db.TaskAnswers.SingleAsync(item => item.TaskId == taskId);
            Assert.Equal("REG-1026-17", answer.Text);
            Assert.Equal(DocumentStatuses.Draft, (await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-007")).Status);
            Assert.Equal(DocumentStatuses.Draft, (await db.CaseDocuments.SingleAsync(document => document.Reference == "CH-I-DOC-011")).Status);
        }
    }

    [Fact]
    public async Task A_document_reference_owned_by_another_case_stops_the_initializer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

        await using (var db = new WeddingBankDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var other = CaseFile.Open(Guid.CreateVersion7(), "OTHER-001", "Another file", "Held with the house.", null, Audiences.Both, 0, now);
            other.AddDocument(Guid.CreateVersion7(), "CH-I-DOC-001", "A paper", "Record", null, "Not chapter one.", Audiences.Both, 0, now);
            db.CaseFiles.Add(other);
            await db.SaveChangesAsync();
            var report = await new ChapterOneInitializer(db, new FixedTime(now)).EnsureAsync();
            Assert.Contains("CH-I-DOC-001", report.Conflicts.Single());
            Assert.Empty(report.Lines);
        }

        await using var check = new WeddingBankDbContext(options);
        Assert.Equal(0, await check.CaseFiles.CountAsync(file => file.Reference == "CH-I-001"));
        Assert.Equal(0, await check.Quests.CountAsync());
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
