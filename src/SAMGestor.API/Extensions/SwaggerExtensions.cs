using Microsoft.OpenApi.Models;

namespace SAMGestor.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SAMGestor API",
                Version = "v1"
            });

            // Exemplo: adicionar segurança via JWT futuramente, se necessário
            // options.AddSecurityDefinition(...)
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SAMGestor API V1");
            options.RoutePrefix = string.Empty; // Swagger na raiz
        });

        return app;
    }
}