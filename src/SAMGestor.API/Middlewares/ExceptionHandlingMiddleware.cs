using System.Net;
using System.Text.Json;
using SAMGestor.Domain.Exceptions;

namespace SAMGestor.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule exception");

            await WriteErrorResponse(context, HttpStatusCode.Conflict, ex.Message, "BUSINESS_RULE_VIOLATION");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");

            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Unauthorized", "UNAUTHORIZED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Internal server error", "INTERNAL_ERROR");
        }
    }

    private async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message, string code)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            error = message,
            code
        });

        await context.Response.WriteAsync(result);
    }
}