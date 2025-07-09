using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.Domain.Interfaces; 
using SAMGestor.Infrastructure.Services; 

namespace SAMGestor.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
    
        services.AddDbContext<SAMContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        
        services.AddScoped<IRelationshipService, HeuristicRelationshipService>();
        
        return services;
    }
}