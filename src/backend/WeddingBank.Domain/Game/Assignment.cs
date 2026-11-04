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

    public bool Completed { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Assignment ForParticipant(Guid id, string code, string login, DateTimeOffset now)
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
            UpdatedAt = now,
        };
    }

    public static Assignment ForTheHouse(Guid id, string code, DateTimeOffset now)
    {
        return new Assignment
        {
            Id = id,
            Code = RequiredCode(code),
            Scope = ProgressScope.Shared,
            UpdatedAt = now,
        };
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
