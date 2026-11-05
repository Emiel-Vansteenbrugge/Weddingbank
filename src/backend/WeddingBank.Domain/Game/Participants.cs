namespace WeddingBank.Domain.Game;

public static class Participants
{
    public const string Juul = "Juul-V";
    public const string Amber = "Amber-D";

    public static readonly string[] Logins = [Juul, Amber];

    public static bool IsParticipant(string? login)
    {
        return login is Juul or Amber;
    }
}

public enum ProgressScope
{
    Individual,
    Shared,
}

public static class ProgressScopes
{
    public const string Individual = "INDIVIDUAL";
    public const string Shared = "SHARED";

    public static string ToName(ProgressScope scope)
    {
        return scope == ProgressScope.Shared ? Shared : Individual;
    }

    public static bool TryParse(string? value, out ProgressScope scope)
    {
        var name = value?.Trim().ToUpperInvariant();
        if (name is Individual or Shared)
        {
            scope = Parse(name);
            return true;
        }

        scope = default;
        return false;
    }

    public static ProgressScope Parse(string name)
    {
        return name switch
        {
            Shared => ProgressScope.Shared,
            Individual => ProgressScope.Individual,
            _ => throw new InvalidOperationException($"Unknown progress scope '{name}'."),
        };
    }
}

public static class GameStatuses
{
    public const string NotStarted = "NOT_STARTED";
    public const string Running = "RUNNING";
}
