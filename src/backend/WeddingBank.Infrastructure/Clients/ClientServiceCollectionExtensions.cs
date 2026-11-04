using Microsoft.Extensions.DependencyInjection;
using WeddingBank.Application.Clients;

namespace WeddingBank.Infrastructure.Clients;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddClientDirectory(this IServiceCollection services)
    {
        services.AddSingleton<IClientReferenceSource, RandomClientReferenceSource>();
        services.AddScoped<IClientStore, EfClientStore>();
        services.AddScoped<ClientService>();
        return services;
    }
}
