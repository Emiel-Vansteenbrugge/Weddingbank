namespace WeddingBank.Domain.Game;

public sealed class Assignment
{
    private Assignment()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = "";

    public ProgressScope Scope { get; private set; }

    public string? ParticipantLogin { get; private set; }

    public Guid? TaskId { get; private set; }

    public bool Completed { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Assignment ForParticipant(Guid id, string code, string login, DateTimeOffset now, Guid? taskId = null)
    {
        if (!Participants.IsParticipant(login))
        {
            throw new ArgumentException("An individual assignment belongs to Juul or Amber.", nameof(login));
        }

        return new Assignment
        {
            Id = id,
            Code = RequiredCode(code),
            Scope = ProgressScope.Individual,
            ParticipantLogin = login,
            TaskId = taskId,
            UpdatedAt = now,
        };
    }

    public static Assignment ForTheHouse(Guid id, string code, DateTimeOffset now, Guid? taskId = null)
    {
        return new Assignment
        {
            Id = id,
            Code = RequiredCode(code),
            Scope = ProgressScope.Shared,
            TaskId = taskId,
            UpdatedAt = now,
        };
    }

    public void Rename(string code, DateTimeOffset now)
    {
        Code = RequiredCode(code);
        UpdatedAt = now;
    }

    public void LinkTask(Guid taskId, DateTimeOffset now)
    {
        if (TaskId == taskId)
        {
            return;
        }

        if (TaskId is not null)
        {
            throw new InvalidOperationException("This assignment already belongs to another task.");
        }

        TaskId = taskId;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        Completed = true;
        UpdatedAt = now;
    }

    public bool CountsFor(string login)
    {
        return Scope == ProgressScope.Shared || ParticipantLogin == login;
    }

    private static string RequiredCode(string code)
    {
        var value = code.Trim();
        if (value.Length is < 1 or > 80)
        {
            throw new ArgumentException("An assignment needs a code.", nameof(code));
        }

        return value;
    }
}
