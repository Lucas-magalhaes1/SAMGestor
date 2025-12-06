using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SAMGestor.API.Auth;
using SAMGestor.API.Services;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces.Auth;
namespace SAMGestor.API.Extensions;

/// <summary>
/// Registra JWT + Policies + CurrentUser. NÃO cria FallbackPolicy,
/// então nada é bloqueado até você decorar as rotas.
/// </summary>
public static class AuthExtensions
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Options (Jwt)
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

        // IHttpContextAccessor + CurrentUser
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Authentication (JWT)
        var jwtSection = config.GetSection(JwtOptions.SectionName);
        var jwt = jwtSection.Get<JwtOptions>();

        if (jwt is null)
        {
            throw new InvalidOperationException(
                $"Config section '{JwtOptions.SectionName}' não foi encontrada. " +
                "Verifique se a seção Jwt está configurada (appsettings, user-secrets ou variáveis de ambiente).");
        }

        if (string.IsNullOrWhiteSpace(jwt.Secret))
        {
            throw new InvalidOperationException(
                "JWT Secret não configurado (Jwt:Secret). " +
                "Defina Jwt__Secret nas variáveis de ambiente ou Jwt:Secret no appsettings/user-secrets.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),

                    // Mapear para os nomes padrão de ClaimTypes
                    NameClaimType = ClaimTypes.NameIdentifier, // mapearemos "sub" ao emitir o token
                    RoleClaimType = ClaimTypes.Role            // mapearemos "role"
                };
            });

        // Authorization (Policies)
        services.AddAuthorization(options =>
        {
            // Policy: Qualquer usuário autenticado (apenas JWT válido)
            options.AddPolicy(Policies.Authenticated, p => p.RequireAuthenticatedUser());
    
            // Policy: Consultant + Manager + Admin (leitura)
            options.AddPolicy(Policies.ReadOnly, p => p.Requirements.Add(new ReadOnlyRequirement()));
    
            // Policy: Manager + Admin
            options.AddPolicy(Policies.ManagerOrAbove, p => p.Requirements.Add(new ManagerOrAboveRequirement()));
    
            // Policy: Só Admin
            options.AddPolicy(Policies.AdminOnly, p => p.Requirements.Add(new AdminOnlyRequirement()));
    
            // Policy: E-mail confirmado
            options.AddPolicy(Policies.EmailConfirmed, p => p.Requirements.Add(new EmailConfirmedRequirement()));
        });

        // Handlers
        services.AddScoped<IAuthorizationHandler, ReadOnlyHandler>();
        services.AddScoped<IAuthorizationHandler, ManagerOrAboveHandler>();
        services.AddScoped<IAuthorizationHandler, AdminOnlyHandler>();
        services.AddScoped<IAuthorizationHandler, EmailConfirmedHandler>();

        return services;
    }

    public static IApplicationBuilder UseAuthInfrastructure(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
