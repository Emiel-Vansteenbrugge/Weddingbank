namespace WeddingBank.Domain.Clients;

public static class ClientReferenceCode
{
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int Length = 12;

    public static string Format(ReadOnlySpan<byte> bytes)
    {
        if (Alphabet.Length != 32)
        {
            throw new InvalidOperationException("The client reference alphabet must contain 32 characters.");
        }

        if (bytes.Length < 8)
        {
            throw new ArgumentException("A client reference needs 8 random bytes.", nameof(bytes));
        }

        Span<char> buffer = stackalloc char[Length];
        buffer[0] = 'A';
        buffer[1] = 'J';
        buffer[2] = '-';
        buffer[7] = '-';
        ReadOnlySpan<int> positions = [3, 4, 5, 6, 8, 9, 10, 11];
        for (var index = 0; index < positions.Length; index++)
        {
            buffer[positions[index]] = Alphabet[bytes[index] & 31];
        }

        return new string(buffer);
    }

    public static bool IsCanonical(string? value)
    {
        if (value is null || value.Length != Length || !value.StartsWith("AJ-", StringComparison.Ordinal) || value[7] != '-')
        {
            return false;
        }

        ReadOnlySpan<int> positions = [3, 4, 5, 6, 8, 9, 10, 11];
        foreach (var position in positions)
        {
            if (Alphabet.IndexOf(value[position]) < 0)
            {
                return false;
            }
        }

        return true;
    }
}
