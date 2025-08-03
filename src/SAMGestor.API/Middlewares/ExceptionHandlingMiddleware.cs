using System.Net;
using System.Text.Json;
using FluentValidation;
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            
            var messages = ex.Errors
                .Select(e => e.ErrorMessage)
                .Distinct();

            await WriteErrorResponse(
                context,
                HttpStatusCode.BadRequest,
                string.Join("; ", messages),
                "VALIDATION_ERROR"
            );
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation");

            await WriteErrorResponse(
                context,
                HttpStatusCode.BadRequest,        
                ex.Message,
                "BUSINESS_RULE_VIOLATION"
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");

            await WriteErrorResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "UNAUTHORIZED"
            );
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");

            await WriteErrorResponse(
                context,
                HttpStatusCode.NotFound,
                ex.Message,
                "NOT_FOUND"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            
            await WriteErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                "Internal server error",
                "INTERNAL_ERROR"
            );
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message, string code)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            error   = message,
            code    = code
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
