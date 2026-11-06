using WeddingBank.Domain.Identity;

namespace WeddingBank.Application.Identity;

public interface IIdentityStore
{
    Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken);

    Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AuthSession?> FindSessionAsync(Guid id, CancellationToken cancellationToken);

    void AddSession(AuthSession session);

    void AddAuditEvent(AuditEvent auditEvent);

    Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
