using System.Net;
using System.Text.Json;
using SAMGestor.Domain.Exceptions;

namespace SAMGestor.API.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Business rule exception");

            await WriteErrorResponse(context, HttpStatusCode.Conflict, ex.Message, "BUSINESS_RULE_VIOLATION");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access");

            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Unauthorized", "UNAUTHORIZED");
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message, "NOT_FOUND");   
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");

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