namespace WeddingBank.Application.Identity;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);

    void VerifyUnknownUser(string password);
}
