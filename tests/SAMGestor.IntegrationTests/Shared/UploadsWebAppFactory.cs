using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace SAMGestor.IntegrationTests.Shared;

public class UploadsWebAppFactory : WebApplicationFactory<Program>
{
    public string TempBasePath { get; } = Path.Combine(Path.GetTempPath(), $"uploads_{Guid.NewGuid()}");
    public string PublicBaseUrl { get; } = "http://localhost/uploads";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // remove registro anterior (se houver)
            var desc = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (desc is not null) services.Remove(desc);

            Directory.CreateDirectory(TempBasePath);
            services.AddSingleton<IStorageService>(sp =>
                new LocalStorageService(TempBasePath, PublicBaseUrl)
            );
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (Directory.Exists(TempBasePath)) Directory.Delete(TempBasePath, recursive: true); }
        catch { /* ignore em CI */ }
    }
}