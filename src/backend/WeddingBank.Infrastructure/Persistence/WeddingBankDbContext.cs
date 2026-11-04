using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Game;
using WeddingBank.Domain.Identity;

namespace WeddingBank.Infrastructure.Persistence;

public sealed class WeddingBankDbContext(DbContextOptions<WeddingBankDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Notice> Notices => Set<Notice>();

    public DbSet<PhoneCall> PhoneCalls => Set<PhoneCall>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(role => role.Name).HasMaxLength(32).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasData(
                new Role(RoleNames.AdminId, RoleNames.Admin),
                new Role(RoleNames.ClientId, RoleNames.Client));
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.Login).HasMaxLength(LoginMax).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.SecurityStamp).HasMaxLength(128).IsRequired();
            entity.Property(user => user.TotpSecretProtected).HasMaxLength(1024);
            entity.HasIndex(user => user.Login).IsUnique();
            entity.HasMany(user => user.Roles)
                .WithOne()
                .HasForeignKey(link => link.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(user => user.Roles)
                .HasField("_roles")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(link => new { link.UserId, link.RoleId });
            entity.HasOne(link => link.Role)
                .WithMany()
                .HasForeignKey(link => link.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.Property(session => session.SecurityStamp).HasMaxLength(128).IsRequired();
            entity.HasIndex(session => session.UserId);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(audit => audit.EventType).HasMaxLength(80).IsRequired();
            entity.Property(audit => audit.TargetType).HasMaxLength(80);
            entity.Property(audit => audit.TargetId).HasMaxLength(80);
            entity.Property(audit => audit.IpAddress).HasMaxLength(64);
            entity.HasIndex(audit => audit.OccurredAt);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(audit => audit.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.Property(item => item.Code).HasMaxLength(80).IsRequired();
            entity.Property(item => item.ParticipantLogin).HasMaxLength(LoginMax);
            entity.Property(item => item.Scope)
                .HasConversion(scope => ProgressScopes.ToName(scope), name => ProgressScopes.Parse(name))
                .HasMaxLength(16)
                .IsRequired();
            entity.HasIndex(item => new { item.Scope, item.ParticipantLogin, item.Code });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Assignments_Scope",
                "(\"Scope\" = 'SHARED' AND \"ParticipantLogin\" IS NULL) OR (\"Scope\" = 'INDIVIDUAL' AND \"ParticipantLogin\" IN ('juul', 'amber'))"));
        });

        modelBuilder.Entity<Notice>(entity =>
        {
            entity.Property(notice => notice.Kind).HasMaxLength(16).IsRequired();
            entity.Property(notice => notice.ParticipantLogin).HasMaxLength(LoginMax);
            entity.Property(notice => notice.Body).HasMaxLength(4000).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Notices_Audience",
                "\"Kind\" IN ('HINT', 'MESSAGE') AND (\"ParticipantLogin\" IS NULL OR \"ParticipantLogin\" IN ('juul', 'amber'))"));
        });

        modelBuilder.Entity<PhoneCall>(entity =>
        {
            entity.Property(call => call.ParticipantLogin).HasMaxLength(LoginMax).IsRequired();
            entity.Property(call => call.Summary).HasMaxLength(500).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PhoneCalls_Participant",
                "\"ParticipantLogin\" IN ('juul', 'amber')"));
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.Property(activity => activity.Kind).HasMaxLength(80).IsRequired();
            entity.Property(activity => activity.ParticipantLogin).HasMaxLength(LoginMax);
            entity.Property(activity => activity.Summary).HasMaxLength(240).IsRequired();
            entity.Property(activity => activity.Scope)
                .HasConversion(scope => ProgressScopes.ToName(scope), name => ProgressScopes.Parse(name))
                .HasMaxLength(16)
                .IsRequired();
            entity.HasIndex(activity => activity.OccurredAt);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.Property(submission => submission.ParticipantLogin).HasMaxLength(LoginMax).IsRequired();
            entity.Property(submission => submission.Answer).HasMaxLength(2000).IsRequired();
            entity.Property(submission => submission.Outcome).HasMaxLength(16).IsRequired();
            entity.HasIndex(submission => submission.Outcome);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Submissions_Outcome",
                "\"Outcome\" IN ('PENDING', 'ACCEPTED', 'REFUSED') AND \"ParticipantLogin\" IN ('juul', 'amber')"));
        });
    }

    private const int LoginMax = 80;
}
