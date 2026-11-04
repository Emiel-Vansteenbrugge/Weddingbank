using System.Security.Cryptography;
using WeddingBank.Application.Clients;
using WeddingBank.Domain.Clients;

namespace WeddingBank.Infrastructure.Clients;

public sealed class RandomClientReferenceSource : IClientReferenceSource
{
    public string Create()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return ClientReferenceCode.Format(bytes);
    }
}
