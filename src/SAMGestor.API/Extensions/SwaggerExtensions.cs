using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace SAMGestor.API.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SAMGestor API",
                    Version = "v1"
                }); 
                c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAMGestor API v1");
                c.RoutePrefix = "swagger";
            });
            return app;
        }
    }
}