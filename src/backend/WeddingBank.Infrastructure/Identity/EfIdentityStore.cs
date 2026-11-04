using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Identity;

public sealed class EfIdentityStore(WeddingBankDbContext db) : IIdentityStore
{
    public Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return Users().SingleOrDefaultAsync(user => user.Login == login, cancellationToken);
    }

    public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Users().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<AuthSession?> FindSessionAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.AuthSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);
    }

    public void AddSession(AuthSession session)
    {
        db.AuthSessions.Add(session);
    }

    public void AddAuditEvent(AuditEvent auditEvent)
    {
        db.AuditEvents.Add(auditEvent);
    }

    public async Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken)
    {
        return await (
                from audit in db.AuditEvents.AsNoTracking()
                join user in db.Users.AsNoTracking() on audit.ActorUserId equals user.Id into actors
                from actor in actors.DefaultIfEmpty()
                orderby audit.OccurredAt descending, audit.Id descending
                select new AuditEntry(
                    audit.Id,
                    audit.OccurredAt,
                    audit.ActorUserId,
                    actor == null ? null : actor.Login,
                    audit.EventType,
                    audit.TargetType,
                    audit.TargetId,
                    audit.IpAddress,
                    audit.MetadataJson))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<User> Users()
    {
        return db.Users.Include(user => user.Roles).ThenInclude(link => link.Role);
    }
}
