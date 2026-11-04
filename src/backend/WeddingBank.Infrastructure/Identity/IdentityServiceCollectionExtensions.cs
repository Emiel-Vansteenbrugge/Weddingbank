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
        services.AddSingleton<IHousePasswordHint, ConfiguredHousePasswords>();
        services.AddScoped<IIdentityStore, EfIdentityStore>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<HouseAccountService>();
        services.AddScoped<DevelopmentUserSeeder>();
        services.AddScoped<HouseAccountBootstrap>();

        var connectionString = DatabaseConnection.Resolve(
            configuration.GetConnectionString("WeddingBank"),
            configuration["POSTGRES_PASSWORD"],
            configuration["POSTGRES_HOST"],
            configuration["POSTGRES_DB"],
            configuration["POSTGRES_USER"],
            isDevelopment);

        services.AddDbContext<WeddingBankDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
