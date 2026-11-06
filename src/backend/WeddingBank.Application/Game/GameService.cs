using WeddingBank.Application.Identity;
using WeddingBank.Domain.Game;

namespace WeddingBank.Application.Game;

public sealed record ParticipantCard(
    string Login,
    string DisplayName,
    string Stage,
    int CompletedTasks,
    int AvailableTasks,
    int HintsSent,
    DateTimeOffset? LastActivity);

public sealed record SharedCard(string Stage, int CompletedTasks, int AvailableTasks, int HintsSent);

public sealed record GameOverview(
    string Status,
    ParticipantCard Juul,
    ParticipantCard Amber,
    SharedCard Shared,
    int OutstandingSubmissions,
    int PendingPhoneRequests);

public sealed record AccountView(string Holder, string Title, string Note);

public sealed record ParticipantDesk(
    string Login,
    string DisplayName,
    string Stage,
    AccountView Account,
    int CompletedTasks,
    int AvailableTasks,
    int SharedCompletedTasks,
    int SharedOpenTasks,
    int HintsWaiting);

public sealed class GameService(IIdentityStore identities, IGameStore games)
{
    public async Task<GameOverview> OverviewAsync(CancellationToken cancellationToken)
    {
        var counts = await games.ReadCountsAsync(cancellationToken);
        var juul = await identities.FindUserByLoginAsync(Participants.Juul, cancellationToken);
        var amber = await identities.FindUserByLoginAsync(Participants.Amber, cancellationToken);
        return new GameOverview(
            GameStatus(counts),
            Card(Participants.Juul, juul?.DisplayName ?? "Juul", counts.JuulCompleted, counts.JuulOpen, counts.HintsForJuul, counts.JuulLastActivity),
            Card(Participants.Amber, amber?.DisplayName ?? "Amber", counts.AmberCompleted, counts.AmberOpen, counts.HintsForAmber, counts.AmberLastActivity),
            new SharedCard(Stage(counts.SharedCompleted, counts.SharedOpen, null), counts.SharedCompleted, counts.SharedOpen, counts.HintsForBoth),
            counts.OutstandingSubmissions,
            counts.PendingPhoneRequests);
    }

    public async Task<ParticipantDesk?> DeskAsync(string login, CancellationToken cancellationToken)
    {
        var normalized = LoginName.Normalize(login);
        if (normalized is null || !Participants.IsParticipant(normalized))
        {
            return null;
        }

        login = normalized;

        var counts = await games.ReadCountsAsync(cancellationToken);
        var visible = await games.ReadVisibleProgressAsync(login, cancellationToken);
        var user = await identities.FindUserByLoginAsync(login, cancellationToken);
        var name = user?.DisplayName ?? (login == Participants.Juul ? "Juul" : "Amber");
        var hints = login == Participants.Juul ? counts.HintsForJuul : counts.HintsForAmber;
        var last = login == Participants.Juul ? counts.JuulLastActivity : counts.AmberLastActivity;
        return new ParticipantDesk(
            login,
            name,
            Stage(visible.Completed, visible.Open, last),
            new AccountView(name, "Private account", "Nothing has been posted."),
            visible.Completed,
            visible.Open,
            visible.SharedCompleted,
            visible.SharedOpen,
            hints + counts.HintsForBoth);
    }

    private static ParticipantCard Card(
        string login,
        string displayName,
        int completed,
        int available,
        int hints,
        DateTimeOffset? lastActivity)
    {
        return new ParticipantCard(login, displayName, Stage(completed, available, lastActivity), completed, available, hints, lastActivity);
    }

    public static string Stage(int completed, int available, DateTimeOffset? lastActivity)
    {
        return completed == 0 && available == 0 && lastActivity is null ? "Not started" : "In progress";
    }

    public static string GameStatus(ProgressCounts counts)
    {
        var quiet = counts.JuulCompleted + counts.JuulOpen + counts.AmberCompleted + counts.AmberOpen
            + counts.SharedCompleted + counts.SharedOpen + counts.HintsForJuul + counts.HintsForAmber
            + counts.HintsForBoth + counts.PendingPhoneRequests + counts.OutstandingSubmissions == 0
            && counts.JuulLastActivity is null
            && counts.AmberLastActivity is null;
        return quiet ? GameStatuses.NotStarted : GameStatuses.Running;
    }
}
