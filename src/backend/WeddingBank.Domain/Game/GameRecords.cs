namespace WeddingBank.Domain.Game;

public sealed class Notice
{
    private Notice()
    {
    }

    public Guid Id { get; private set; }

    public string Kind { get; private set; } = "";

    public string? ParticipantLogin { get; private set; }

    public string Body { get; private set; } = "";

    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static Notice Hint(Guid id, string? participantLogin, string body, DateTimeOffset sentAt)
    {
        return Create(id, "HINT", participantLogin, body, sentAt);
    }

    public static Notice Message(Guid id, string? participantLogin, string body, DateTimeOffset sentAt)
    {
        return Create(id, "MESSAGE", participantLogin, body, sentAt);
    }

    public bool IsFor(string login)
    {
        return ParticipantLogin is null || ParticipantLogin == login;
    }

    private static Notice Create(Guid id, string kind, string? participantLogin, string body, DateTimeOffset sentAt)
    {
        if (participantLogin is not null && !Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("A notice is for Juul, Amber, or both.", nameof(participantLogin));
        }

        var text = body.Trim();
        if (text.Length is < 1 or > 4000)
        {
            throw new ArgumentException("A notice needs a text.", nameof(body));
        }

        return new Notice
        {
            Id = id,
            Kind = kind,
            ParticipantLogin = participantLogin,
            Body = text,
            SentAt = sentAt,
        };
    }
}

public sealed class PhoneCall
{
    private PhoneCall()
    {
    }

    public Guid Id { get; private set; }

    public string ParticipantLogin { get; private set; } = "";

    public string Summary { get; private set; } = "";

    public bool Handled { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? HandledAt { get; private set; }

    public static PhoneCall Request(Guid id, string participantLogin, string summary, DateTimeOffset requestedAt)
    {
        if (!Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("A telephone request belongs to Juul or Amber.", nameof(participantLogin));
        }

        var text = summary.Trim();
        if (text.Length is < 1 or > 500)
        {
            throw new ArgumentException("A telephone request needs a summary.", nameof(summary));
        }

        return new PhoneCall
        {
            Id = id,
            ParticipantLogin = participantLogin,
            Summary = text,
            RequestedAt = requestedAt,
        };
    }
}

public sealed class Activity
{
    private Activity()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string Kind { get; private set; } = "";

    public string? ParticipantLogin { get; private set; }

    public ProgressScope Scope { get; private set; }

    public string Summary { get; private set; } = "";

    public static Activity Record(
        Guid id,
        DateTimeOffset occurredAt,
        string kind,
        string? participantLogin,
        ProgressScope scope,
        string summary)
    {
        if (participantLogin is not null && !Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("An activity belongs to Juul, Amber, or the house.", nameof(participantLogin));
        }

        var text = summary.Trim();
        if (kind.Trim().Length is < 1 or > 80 || text.Length is < 1 or > 240)
        {
            throw new ArgumentException("An activity needs a kind and a summary.");
        }

        return new Activity
        {
            Id = id,
            OccurredAt = occurredAt,
            Kind = kind.Trim(),
            ParticipantLogin = participantLogin,
            Scope = scope,
            Summary = text,
        };
    }
}

public sealed class Submission
{
    private Submission()
    {
    }

    public Guid Id { get; private set; }

    public Guid AssignmentId { get; private set; }

    public string ParticipantLogin { get; private set; } = "";

    public string Answer { get; private set; } = "";

    public string Outcome { get; private set; } = "";

    public DateTimeOffset SubmittedAt { get; private set; }

    public static Submission Offer(Guid id, Guid assignmentId, string participantLogin, string answer, DateTimeOffset submittedAt)
    {
        if (!Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("A submission belongs to Juul or Amber.", nameof(participantLogin));
        }

        var text = answer.Trim();
        if (text.Length is < 1 or > 2000)
        {
            throw new ArgumentException("A submission needs an answer.", nameof(answer));
        }

        return new Submission
        {
            Id = id,
            AssignmentId = assignmentId,
            ParticipantLogin = participantLogin,
            Answer = text,
            Outcome = "PENDING",
            SubmittedAt = submittedAt,
        };
    }

    public static Submission Decide(Guid id, Guid assignmentId, string participantLogin, string answer, bool accepted, DateTimeOffset submittedAt)
    {
        if (!Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("A submission belongs to Juul or Amber.", nameof(participantLogin));
        }

        if (string.IsNullOrEmpty(answer) || answer.Length > 2000)
        {
            throw new ArgumentException("A submission needs an answer.", nameof(answer));
        }

        return new Submission
        {
            Id = id,
            AssignmentId = assignmentId,
            ParticipantLogin = participantLogin,
            Answer = answer,
            Outcome = accepted ? "ACCEPTED" : "REFUSED",
            SubmittedAt = submittedAt,
        };
    }
}
