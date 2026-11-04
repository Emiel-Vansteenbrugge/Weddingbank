using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Clients;
using WeddingBank.Domain.Identity;

namespace WeddingBank.Infrastructure.Persistence;

public sealed class WeddingBankDbContext(DbContextOptions<WeddingBankDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ClientUser> ClientUsers => Set<ClientUser>();

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

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(client => client.ClientReference).HasMaxLength(ClientReferenceCode.Length).IsRequired();
            entity.Property(client => client.LegalName).HasMaxLength(ClientFields.LegalNameMax).IsRequired();
            entity.Property(client => client.DisplayName).HasMaxLength(ClientFields.DisplayNameMax);
            entity.Property(client => client.EmailAddress).HasMaxLength(ClientFields.EmailMax).IsRequired();
            entity.Property(client => client.PhoneNumber).HasMaxLength(ClientFields.PhoneMax);
            entity.Property(client => client.AddressLine1).HasMaxLength(ClientFields.AddressMax);
            entity.Property(client => client.AddressLine2).HasMaxLength(ClientFields.AddressMax);
            entity.Property(client => client.PostalCode).HasMaxLength(ClientFields.PostalMax);
            entity.Property(client => client.City).HasMaxLength(ClientFields.CityMax);
            entity.Property(client => client.Country).HasMaxLength(ClientFields.CountryMax);
            entity.Property(client => client.Status)
                .HasConversion(status => ClientStatuses.ToName(status), name => ParseStatus(name))
                .HasMaxLength(16)
                .IsRequired();
            entity.HasIndex(client => client.ClientReference).IsUnique();
            entity.HasIndex(client => client.CreatedAt);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Clients_Status",
                "\"Status\" IN ('ACTIVE', 'INACTIVE', 'SUSPENDED')"));
        });

        modelBuilder.Entity<ClientUser>(entity =>
        {
            entity.HasKey(link => new { link.ClientId, link.UserId });
            entity.HasIndex(link => link.UserId);
            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(link => link.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(link => link.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static ClientStatus ParseStatus(string name)
    {
        if (!ClientStatuses.TryParse(name, out var status))
        {
            throw new InvalidOperationException($"Unknown client status '{name}'.");
        }

        return status;
    }

    private const int LoginMax = 80;
}
