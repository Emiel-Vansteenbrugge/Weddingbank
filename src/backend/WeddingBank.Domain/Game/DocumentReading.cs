namespace WeddingBank.Domain.Game;

public sealed class DocumentReading
{
    private DocumentReading()
    {
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public string ParticipantLogin { get; private set; } = "";

    public DateTimeOffset ReadAt { get; private set; }

    public static DocumentReading Open(Guid id, Guid documentId, string participantLogin, DateTimeOffset readAt)
    {
        if (!Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("A document reading belongs to Juul or Amber.", nameof(participantLogin));
        }

        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("A document reading needs a document.", nameof(documentId));
        }

        return new DocumentReading
        {
            Id = id,
            DocumentId = documentId,
            ParticipantLogin = participantLogin,
            ReadAt = readAt,
        };
    }
}

public sealed class DocumentReadBaseline
{
    private DocumentReadBaseline()
    {
    }

    public int Id { get; private set; }

    public DateTimeOffset EstablishedAt { get; private set; }

    public static DocumentReadBaseline Establish(DateTimeOffset establishedAt)
    {
        return new DocumentReadBaseline
        {
            Id = 1,
            EstablishedAt = establishedAt,
        };
    }
}
