using WeddingBank.Domain.Game;

namespace WeddingBank.Application.Game;

public interface IQuestStore
{
    Task<IReadOnlyList<Quest>> ListAsync(CancellationToken cancellationToken);

    Task<Quest?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Quest?> FindByTaskAsync(Guid taskId, CancellationToken cancellationToken);

    Task<bool> ReferenceInUseAsync(string reference, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> TaskReferenceInUseAsync(string reference, Guid? exceptTaskId, CancellationToken cancellationToken);

    Task<bool> CaseExistsAsync(Guid caseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DocumentsOnCaseAsync(Guid caseId, IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, (string Reference, string Title)>> DescribeDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Assignment>> AssignmentsForAsync(IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken);

    Task<int> CountSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Submission>> SubmissionsForQuestAsync(Guid questId, CancellationToken cancellationToken);

    void Add(Quest quest);

    void AddAssignment(Assignment assignment);

    void RemoveAssignment(Assignment assignment);

    void AddSubmission(Submission submission);

    Task SaveAsync(CancellationToken cancellationToken);
}
