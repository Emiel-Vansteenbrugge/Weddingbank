using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeddingBank.Application.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddWeddingBankIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.AddOptions<AuthOptions>().Bind(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IIdentityStore, EfIdentityStore>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<HouseAccountService>();
        services.AddScoped<DevelopmentUserSeeder>();

        var connectionString = configuration.GetConnectionString("WeddingBank");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = isDevelopment
                ? WeddingBankDbContextFactory.LocalConnectionString
                : "Host=invalid;Database=unset;Username=unset;Password=unset";
        }

        services.AddDbContext<WeddingBankDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
