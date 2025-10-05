using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Service.Spaces.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Services;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Messaging.Outbox;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;
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
        services
            .AddPersistence(configuration)
            .AddMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddPersistence(
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
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IFamilyMemberRepository, FamilyMemberRepository>();
        
        services.AddScoped<ServiceSpacesSeeder>();
        
        services.AddMediatR(typeof(CreateRetreatHandler).Assembly);
        services.AddValidatorsFromAssemblyContaining<CreateRetreatValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateRegistrationValidator>();
        services.AddScoped<IServiceSpaceRepository, ServiceSpaceRepository>();
        services.AddScoped<IServiceRegistrationRepository, ServiceRegistrationRepository>();
        services.AddScoped<IServiceAssignmentRepository, ServiceAssignmentRepository>();
        services.AddValidatorsFromAssemblyContaining<CreateServiceSpaceValidator>();

        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mqOpt = new RabbitMqOptions
        {
            HostName = configuration["MessageBus:Host"] ?? "rabbitmq",
            UserName = configuration["MessageBus:User"] ?? "guest",
            Password = configuration["MessageBus:Pass"] ?? "guest",
            Exchange = "sam.topic"
        };
        
        services.AddSingleton(mqOpt);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<EventPublisher>();
        
        services.AddScoped<IEventBus, OutboxEventBus>();
        
        services.AddHostedService<OutboxDispatcher>();


        return services;
    }
}
