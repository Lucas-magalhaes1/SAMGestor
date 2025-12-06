using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Extensions
{
    public static class SwaggerExtensions
    {
        private static string SwaggerOrderSelector(ApiDescription apiDesc)
        {
            var cad = apiDesc.ActionDescriptor as ControllerActionDescriptor;

            var methodOrder     = cad?.MethodInfo.GetCustomAttribute<SwaggerOrderAttribute>()?.Order;
            var controllerOrder = cad?.ControllerTypeInfo.GetCustomAttribute<SwaggerOrderAttribute>()?.Order;

            int order = methodOrder ?? controllerOrder ?? 0;

            string group  = apiDesc.GroupName    ?? string.Empty;
            string rel    = apiDesc.RelativePath ?? string.Empty;
            string method = apiDesc.HttpMethod   ?? string.Empty;

            return string.Format(CultureInfo.InvariantCulture, "{0:D4}_{1}_{2}_{3}",
                order, group, rel, method);
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title       = "SAMGestor API",
                    Version     = "v1",
                    Description = "Documentação da API SAMGestor."
                });

                c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
                c.OrderActionsBy(SwaggerOrderSelector);
                c.EnableAnnotations();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "bearer",
                    BearerFormat = "JWT",
                    In           = ParameterLocation.Header,
                    Description  = "Insira o token JWT no formato: Bearer {seu_jwt}"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"  
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAMGestor API v1");
                c.RoutePrefix  = "swagger";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });

            return app;
        }
    }
}
