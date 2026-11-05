using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Api;

internal static class ChapterOneCli
{
    private const string Flag = "--initialize-chapter-i";

    public static bool Requested(string[] args)
    {
        return args.Contains(Flag);
    }

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            using var host = CreateHost(args);
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WeddingBankDbContext>();
            await DatabaseConnection.ApplyAsync(db);
            var report = await scope.ServiceProvider.GetRequiredService<ChapterOneInitializer>().EnsureAsync();
            foreach (var line in report.Lines)
            {
                Console.WriteLine(line);
            }

            foreach (var conflict in report.Conflicts)
            {
                Console.Error.WriteLine(conflict);
            }

            if (report.Conflicts.Count > 0)
            {
                Console.Error.WriteLine("Chapter I initialization stopped because existing records conflict.");
                return 1;
            }

            Console.WriteLine("Chapter I initialization completed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(PublicFailure(exception));
            return 1;
        }
    }

    internal static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ApplicationName = typeof(ChapterOneCli).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environments.Production,
        });
        builder.Services.AddWeddingBankIdentity(builder.Configuration, builder.Environment.IsDevelopment());
        builder.Services.AddGame();
        return builder.Build();
    }

    private static string PublicFailure(Exception exception)
    {
        var message = exception.GetBaseException().Message.ReplaceLineEndings(" ");
        return $"Chapter I initialization failed: {Redact(message)}";
    }

    private static string Redact(string message)
    {
        var builder = new System.Text.StringBuilder(message.Length);
        var index = 0;
        while (index < message.Length)
        {
            var found = message.IndexOf("password=", index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                builder.Append(message, index, message.Length - index);
                break;
            }

            builder.Append(message, index, found - index);
            builder.Append("password=***");
            var end = found + "password=".Length;
            while (end < message.Length && message[end] is not ';' and not ' ' and not '\'' and not '"')
            {
                end++;
            }

            index = end;
        }

        return builder.Length > 240 ? builder.ToString(0, 240) : builder.ToString();
    }
}
