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
        var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
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

        // Authorization (Policies) — registradas mas NÃO exigidas
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.ReadOnly, p => p.Requirements.Add(new ReadOnlyRequirement()));
            options.AddPolicy(Policies.ManageAllButDeleteUsers, p => p.Requirements.Add(new ManageAllButDeleteUsersRequirement()));
            options.AddPolicy(Policies.AdminOnly, p => p.Requirements.Add(new AdminOnlyRequirement()));
            options.AddPolicy(Policies.EmailConfirmed, p => p.Requirements.Add(new EmailConfirmedRequirement()));

            // Importante: NÃO setar options.FallbackPolicy → nada fica protegido por padrão.
        });

        // Handlers
        services.AddScoped<IAuthorizationHandler, ReadOnlyHandler>();
        services.AddScoped<IAuthorizationHandler, ManageAllButDeleteUsersHandler>();
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
