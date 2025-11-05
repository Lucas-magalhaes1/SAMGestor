using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace SAMGestor.IntegrationTests.Shared;

public class UploadsWebAppFactory : PostgresWebAppFactory
{
    public string TempBasePath { get; } = Path.Combine(Path.GetTempPath(), $"uploads_{Guid.NewGuid():N}");
    
    public string PublicBaseUrl { get; } = "http://localhost/uploads";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1) configura Postgres de teste (container + schema)
        base.ConfigureWebHost(builder);

        // 2) sobrescreve apenas o IStorageService para usar pasta temporária
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            if (desc is not null)
                services.Remove(desc);

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
            // ignora erro de limpeza em CI
        }
    }
}