using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SAMGestor.Application.Dtos.Auth;
using SAMGestor.Application.Features.Auth.ConfirmEmail;
using SAMGestor.Application.Features.Auth.Login;
using SAMGestor.Application.Features.Auth.Logout;
using SAMGestor.Application.Features.Auth.Refresh;
using SAMGestor.Application.Features.Auth.RequestPasswordReset;
using SAMGestor.Application.Features.Auth.ResetPassword;
using Swashbuckle.AspNetCore.Annotations;
using RateLimitPolicies = SAMGestor.API.Extensions.RateLimitingExtensions.Policies;

namespace SAMGestor.API.Controllers.Users;

[ApiController]
[Route("api")]
[SwaggerTag("Operações relacionadas à autenticação e autorização de usuários.")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

   /// <summary> Login de usuário. Retorna token de acesso e refresh. </summary>
[HttpPost("login")]
[EnableRateLimiting(RateLimitPolicies.Login)]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
{
    try
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var res = await _mediator.Send(new LoginCommand(body.Email, body.Password, ua, ip), ct);
        return Ok(res);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Falha no login para e-mail: {Email}", body.Email);
        return Unauthorized(new { error = ex.Message });
    }
}

/// <summary> Refresh de token de acesso com rotação. </summary>
[HttpPost("refresh")]
[EnableRateLimiting(RateLimitPolicies.Refresh)]
public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
{
    try
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var res = await _mediator.Send(
            new RefreshCommand(body.AccessToken, body.RefreshToken, ua, ip), ct);
        return Ok(res);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Tentativa não autorizada de refresh");
        return Unauthorized(new { error = ex.Message });
    }
}

/// <summary> Logout de usuário (revoga refresh token). </summary>
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout([FromBody] LogoutRequest body, CancellationToken ct)
{
    try
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value 
            ?? throw new UnauthorizedAccessException("Token inválido"));
        await _mediator.Send(new LogoutCommand(body.RefreshToken, userId), ct);
        return Ok(new { message = "Logout realizado com sucesso" });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Falha no logout");
        return Unauthorized(new { error = ex.Message });
    }
}
    
    /// <summary> Confirmação de e-mail. </summary>
    [HttpPost("auth/confirm-email")]
    [EnableRateLimiting(RateLimitPolicies.EmailConfirmation)]
    public async Task<ActionResult<LoginResponse>> ConfirmEmail([FromBody] ConfirmEmailRequest body, CancellationToken ct)
    {
        try
        {
            var res = await _mediator.Send(new ConfirmEmailCommand(body.Token, body.NewPassword), ct);
            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email confirmado falhou");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary> Solicitação de redefinição de senha. </summary>
    [HttpPost("auth/request-password-reset")]
    [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest body, CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _mediator.Send(new RequestPasswordResetCommand(body.Email, baseUrl), ct);
        return Ok(new { message = "Se o e‑mail existir, um link para redefinição foi enviado." });
    }
    
    /// <summary> Redefinição de senha. </summary>
    [HttpPost("auth/reset-password")]
    [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ResetPasswordCommand(body.Token, body.NewPassword), ct);
            return Ok(new { message = "Senha resetada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redefinição de senha falhou");
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary> Informações do usuário atual. </summary>
    [HttpGet("user")]
    public ActionResult<object> CurrentUser()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { error = "Não autenticado" });

        return Ok(new
        {
            id = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
            email = User.FindFirst("email")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            role = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
            emailConfirmed = User.FindFirst("email_confirmed")?.Value
        });
    }
}
