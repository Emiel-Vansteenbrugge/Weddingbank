using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WeddingBank.Infrastructure.Persistence;

public static class DatabaseConnection
{
    public static string Resolve(
        string? configured,
        string? postgresPassword,
        string? host,
        string? database,
        string? username,
        bool isDevelopment)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (!string.IsNullOrWhiteSpace(postgresPassword))
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = string.IsNullOrWhiteSpace(host) ? "postgres" : host,
                Port = 5432,
                Database = string.IsNullOrWhiteSpace(database) ? "weddingbank" : database,
                Username = string.IsNullOrWhiteSpace(username) ? "weddingbank" : username,
                Password = postgresPassword,
            }.ConnectionString;
        }

        return isDevelopment
            ? WeddingBankDbContextFactory.LocalConnectionString
            : "Host=invalid;Database=unset;Username=unset;Password=unset";
    }

    public static Task ApplyAsync(WeddingBankDbContext db, CancellationToken cancellationToken = default)
    {
        return db.Database.MigrateAsync(cancellationToken);
    }
}
