using Microsoft.Extensions.DependencyInjection;
using WeddingBank.Application.Game;

namespace WeddingBank.Infrastructure.Game;

public static class GameServiceCollectionExtensions
{
    public static IServiceCollection AddGame(this IServiceCollection services)
    {
        services.AddScoped<IGameStore, EfGameStore>();
        services.AddScoped<GameService>();
        services.AddScoped<IHeistStore, EfHeistStore>();
        services.AddScoped<HeistService>();
        services.AddScoped<ICaseStore, EfCaseStore>();
        services.AddScoped<IDocumentReadStore, EfDocumentReadStore>();
        services.AddScoped<CaseService>();
        services.AddScoped<EfQuestStore>();
        services.AddScoped<IQuestStore>(provider => provider.GetRequiredService<EfQuestStore>());
        services.AddScoped<IDocumentGate>(provider => provider.GetRequiredService<EfQuestStore>());
        services.AddScoped<QuestService>();
        services.AddScoped<ChapterOneInitializer>();
        return services;
    }
}
