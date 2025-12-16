using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SAMGestor.Application.Interfaces;

namespace SAMGestor.IntegrationTests.Shared;

public class UploadsWebAppFactory : PostgresWebAppFactory
{
    public string TempBasePath { get; } = Path.Combine(Path.GetTempPath(), $"uploads_{Guid.NewGuid():N}");
    public string PublicBaseUrl { get; } = "http://localhost/uploads";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStorageService>();

            Directory.CreateDirectory(TempBasePath);

            services.AddSingleton<IStorageService>(_ =>
                new LocalStorageService(TempBasePath, PublicBaseUrl)
            );
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (Directory.Exists(TempBasePath))
                Directory.Delete(TempBasePath, recursive: true);
        }
        catch
        {
        }
    }
}