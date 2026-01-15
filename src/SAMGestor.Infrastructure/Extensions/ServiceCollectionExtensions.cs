using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Features.Dashboards.Families;
using SAMGestor.Application.Features.Dashboards.Overview;
using SAMGestor.Application.Features.Dashboards.Payments;
using SAMGestor.Application.Features.Dashboards.Service;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Reports.Templates;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Service.Spaces.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Application.Services;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Messaging.Consumers;
using SAMGestor.Infrastructure.Messaging.Options;
using SAMGestor.Infrastructure.Messaging.Outbox;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.Infrastructure.Repositories;
using SAMGestor.Infrastructure.Repositories.Family;
using SAMGestor.Infrastructure.Repositories.Reports;
using SAMGestor.Infrastructure.Repositories.Retreat;
using SAMGestor.Infrastructure.Repositories.User;
using SAMGestor.Infrastructure.Services;
using SAMGestor.Infrastructure.Services.Reports;
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
            .AddMessaging(configuration)
            .AddStorage(configuration); 

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
        
        services.AddDistributedMemoryCache();
        
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
        services.AddScoped<ITentRepository, TentRepository>();
        services.AddScoped<ITentAssignmentRepository, TentAssignmentRepository>();
        services.AddScoped<IReportCatalog, ReportCatalogServiceRepository>();
        services.AddScoped<IReportEngine, ReportEngineRepository>();
        services.AddScoped<IReportTemplate, ShirtsBySizeTemplate>();
        services.AddScoped<IReportingReadDb, ReportingReadDb>();
        services.AddValidatorsFromAssemblyContaining<Application.Features.Reports.Create.CreateReportValidator>();
        services.AddScoped<IReportTemplate, ContemplatedGeneralTemplate>();
        services.AddScoped<IReportTemplate, ContemplatedByFamilyTemplate>();
        services.AddScoped<IReportTemplate, TentsAllocationsTemplate>();
        services.AddScoped<IReportTemplate, PeopleEpitaphTemplate>();
        services.AddScoped<IReportTemplateRegistry, ReportTemplateRegistryRepository>();
        services.AddScoped<IReportExporter, CsvReportExporter>();
        services.AddScoped<IReportExporter, DocumentReportExporter>();
        services.AddValidatorsFromAssemblyContaining<GetOverviewValidator>();
        services.AddValidatorsFromAssemblyContaining<GetFamiliesValidator>();
        services.AddValidatorsFromAssemblyContaining<GetPaymentsSeriesValidator>();
        services.AddValidatorsFromAssemblyContaining<GetServiceOverviewValidator>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value);
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IJwtTokenDecoder, JwtTokenDecoder>();
        services.AddScoped<IRawSqlExecutor, RawSqlExecutor>();
        services.AddScoped<IManualPaymentProofRepository, ManualPaymentProofRepository>();

       

        services.AddHttpClient<IImageFetcher, HttpImageFetcher>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
#if DEBUG
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // opcional: aceita HTTPS self-signed no DEV (localhost)
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#endif
            ;

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
            Exchange = "sam.topic",
            ServingPaymentQueue = "core.payment.serving"
        };

        var autoOpt = new ServiceAutoAssignOptions();
        configuration.GetSection("ServiceAutoAssign").Bind(autoOpt);
        services.AddSingleton(autoOpt);

        services.AddSingleton(mqOpt);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<EventPublisher>();

        services.AddScoped<IEventBus, OutboxEventBus>();

        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<ServicePaymentConfirmedConsumer>();

        return services;
    }

    //  Storage (MVP local; pronto para trocar por S3/Azure depois) 
    private static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"] ?? "Local";

        if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            var basePath = configuration["Storage:BasePath"] ?? "wwwroot/uploads";
            var publicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? "http://localhost:5000/uploads";

            services.AddSingleton<IStorageService>(_ =>
                new LocalStorageService(basePath, publicBaseUrl));
        }
        else
        {
            throw new NotSupportedException($"Storage provider '{provider}' não suportado no ambiente atual.");
        }

        return services;
    }
}
