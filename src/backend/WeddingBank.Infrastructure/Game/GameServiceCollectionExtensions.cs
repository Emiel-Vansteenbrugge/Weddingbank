using Microsoft.Extensions.DependencyInjection;
using WeddingBank.Application.Game;

namespace WeddingBank.Infrastructure.Game;

public static class GameServiceCollectionExtensions
{
    public static IServiceCollection AddGame(this IServiceCollection services)
    {
        services.AddScoped<IGameStore, EfGameStore>();
        services.AddScoped<GameService>();
        return services;
    }
}
