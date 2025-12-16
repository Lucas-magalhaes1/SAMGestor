using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using SAMGestor.Domain.Exceptions;

namespace SAMGestor.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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

            // Se a resposta já começou, não dá pra escrever JSON de erro
            if (context.Response.HasStarted)
                throw;

            // ✅ Em Test (e Development), devolve o erro real no body
            var includeDetails = _env.IsDevelopment() || _env.IsEnvironment("Test");

            var msg = includeDetails
                ? ex.ToString() // stack trace completo
                : "Internal server error";

            await WriteErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                msg,
                "INTERNAL_ERROR",
                includeDetails ? ex : null
            );
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        string code,
        Exception? ex = null)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            error = message,
            code = code,
            traceId = context.TraceIdentifier,

            // detalhes úteis só quando a gente passar exception
            exception = ex is null ? null : new
            {
                type = ex.GetType().FullName,
                message = ex.Message,
                inner = ex.InnerException?.Message
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
