using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WeddingBank.Infrastructure.Persistence;

public sealed class WeddingBankDbContextFactory : IDesignTimeDbContextFactory<WeddingBankDbContext>
{
    public const string LocalConnectionString =
        "Host=127.0.0.1;Port=5432;Database=weddingbank;Username=weddingbank;Password=weddingbank_dev";

    public WeddingBankDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__WeddingBank");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalConnectionString;
        }

        var options = new DbContextOptionsBuilder<WeddingBankDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new WeddingBankDbContext(options);
    }
}
