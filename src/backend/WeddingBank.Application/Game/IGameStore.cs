namespace WeddingBank.Application.Game;

public sealed record ProgressCounts(
    int JuulCompleted,
    int JuulOpen,
    int AmberCompleted,
    int AmberOpen,
    int SharedCompleted,
    int SharedOpen,
    int HintsForJuul,
    int HintsForAmber,
    int HintsForBoth,
    int PendingPhoneRequests,
    int OutstandingSubmissions,
    DateTimeOffset? JuulLastActivity,
    DateTimeOffset? AmberLastActivity);

public interface IGameStore
{
    Task<ProgressCounts> ReadCountsAsync(CancellationToken cancellationToken);
}
