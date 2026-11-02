using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeddingBank.Application.Enquiries;

namespace WeddingBank.Infrastructure.Enquiries;

public static class ResendServiceCollectionExtensions
{
    public static IServiceCollection AddResend(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ResendOptions>().Bind(configuration.GetSection(ResendOptions.SectionName));
        services.AddHttpClient<IEnquirySender, ResendEnquirySender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        return services;
    }
}
