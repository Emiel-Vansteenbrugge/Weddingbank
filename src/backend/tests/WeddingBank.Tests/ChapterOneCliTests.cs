using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using WeddingBank.Api;

namespace WeddingBank.Tests;

public sealed class ChapterOneCliTests
{
    [Fact]
    public void Initializer_host_does_not_register_an_http_server()
    {
        using var host = ChapterOneCli.CreateHost(["--initialize-chapter-i"]);

        Assert.Null(host.Services.GetService<IServer>());
    }

    [Fact]
    public async Task Initializer_command_exits_without_starting_the_http_server()
    {
        var directory = ApiDirectory();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec \"{Path.Combine(directory, "WeddingBank.Api.dll")}\" --initialize-chapter-i",
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        start.Environment["ConnectionStrings__WeddingBank"] = "Host=127.0.0.1;Port=1;Timeout=1;Database=unused;Username=unused;Password=unused";
        start.Environment["POSTGRES_HOST"] = "127.0.0.1";
        start.Environment["POSTGRES_PASSWORD"] = "unused";

        using var process = Process.Start(start);
        Assert.NotNull(process);
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        var exited = process.WaitForExit(12000);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }

        var output = await stdout + await stderr;
        Assert.True(exited, output);
        Assert.Equal(1, process.ExitCode);
        Assert.Contains("Chapter I initialization failed", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Now listening on", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Application started", output, StringComparison.Ordinal);
        Assert.DoesNotContain("address already in use", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=unused", output, StringComparison.Ordinal);
        Assert.True(listener.Server.IsBound);
    }

    private static string ApiDirectory()
    {
        var location = typeof(Program).Assembly.Location;
        var directory = Path.GetDirectoryName(location) ?? throw new InvalidOperationException("The API assembly has no directory.");
        if (File.Exists(Path.Combine(directory, "WeddingBank.Api.runtimeconfig.json")))
        {
            return directory;
        }

        var configuration = new DirectoryInfo(directory).Parent?.Name ?? "Debug";
        var candidate = Path.GetFullPath(Path.Combine(
            directory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "WeddingBank.Api",
            "bin",
            configuration,
            "net10.0"));
        if (!File.Exists(Path.Combine(candidate, "WeddingBank.Api.dll")))
        {
            throw new InvalidOperationException("The API build output could not be found.");
        }

        return candidate;
    }
}
