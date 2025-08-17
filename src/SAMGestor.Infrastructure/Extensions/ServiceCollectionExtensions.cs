using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Repositories;
using SAMGestor.Infrastructure.Services;
using SAMGestor.Infrastructure.UnitOfWork;

namespace SAMGestor.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        var schema = SAMContext.Schema; 
    
        services.AddDbContext<SAMContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema)
            )
        );
        
        services.AddScoped<IRelationshipService, HeuristicRelationshipService>();
        services.AddScoped<IRetreatRepository, RetreatRepository>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        
        services.AddMediatR(typeof(CreateRetreatHandler).Assembly);
        services.AddValidatorsFromAssemblyContaining<CreateRetreatValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateRegistrationValidator>();
        
        
        
        return services;
    }
}