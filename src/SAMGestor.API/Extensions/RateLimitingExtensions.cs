using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SAMGestor.API.Extensions;

public static class RateLimitingExtensions
{
    public static class Policies
    {
        public const string Login = "login";
        public const string PasswordReset = "password-reset";
        public const string Refresh = "refresh";
        public const string EmailConfirmation = "email-confirmation";
        public const string Strict = "strict";
    }

    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
           
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 1. LOGIN: 5 tentativas por minuto por IP (proteção contra força bruta)
            options.AddPolicy(Policies.Login, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,                   
                        Window = TimeSpan.FromMinutes(1),   
                        SegmentsPerWindow = 6,              
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0                      
                    }));

            // 2. PASSWORD RESET: 3 tentativas por hora por IP
            options.AddPolicy(Policies.PasswordReset, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // 3. REFRESH: 10 tentativas por minuto por IP
            options.AddPolicy(Policies.Refresh, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2                      // permite 2 requisições na fila
                    }));

            // 4. EMAIL CONFIRMATION: 5 tentativas por hora por IP
            options.AddPolicy(Policies.EmailConfirmation, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // 5. STRICT: Para endpoints administrativos sensíveis (3 por minuto)
            options.AddPolicy(Policies.Strict, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Handler customizado para 429 (Too Many Requests)
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Muitas requisições",
                        message = "Limite de requisições excedido. Tente novamente mais tarde.",
                        tentarNovamenteEm = $"{retryAfter.TotalSeconds:F0} segundos"
                    }, cancellationToken: cancellationToken);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Muitas requisições",
                        message = "Limite de requisições excedido. Tente novamente mais tarde."
                    }, cancellationToken: cancellationToken);
                }
            };
        });

        return services;
    }
}
