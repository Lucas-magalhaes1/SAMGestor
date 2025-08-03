using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.API; 

namespace SAMGestor.IntegrationTests.Shared;

public class CustomWebAppFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            
            var descriptor = services.Single(d =>
                d.ServiceType == typeof(DbContextOptions<SAMContext>));
            services.Remove(descriptor);

           
            services.AddDbContext<SAMContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));
            
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<SAMContext>();
            ctx.Database.EnsureCreated();
        });
    }
}