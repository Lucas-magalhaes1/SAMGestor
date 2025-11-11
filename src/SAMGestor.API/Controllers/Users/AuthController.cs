using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Dtos.Auth;
using SAMGestor.Application.Features.Auth.ConfirmEmail;
using SAMGestor.Application.Features.Auth.Login;
using SAMGestor.Application.Features.Auth.RequestPasswordReset;
using SAMGestor.Application.Features.Auth.ResetPassword;

namespace SAMGestor.API.Controllers.Users;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var res = await _mediator.Send(new LoginCommand(body.Email, body.Password, ua, ip), ct);
        return Ok(res);
    }

    // Mock compat: devolve literal "1234" (você pode trocar por JWT real depois)
    [HttpGet("refresh")]
    public ActionResult<RefreshResponse> Refresh()
    {
        return Ok(new RefreshResponse("1234", "1234"));
    }

    [HttpGet("logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        // TODO: opcional revogar refresh atual se enviar header com refresh
        return Ok(new LogoutResponse("Logged out successfully"));
    }

    [HttpPost("auth/confirm-email")]
    public async Task<ActionResult<LoginResponse>> ConfirmEmail([FromBody] ConfirmEmailRequest body, CancellationToken ct)
    {
        var res = await _mediator.Send(new ConfirmEmailCommand(body.Token, body.NewPassword), ct);
        return Ok(res);
    }

    [HttpPost("auth/request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest body, CancellationToken ct)
    {
        // defina a URL base do front (poderia vir do appsettings)
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _mediator.Send(new RequestPasswordResetCommand(body.Email, baseUrl), ct);
        return Ok(new { message = "If the e-mail exists, a reset link was sent." });
    }

    [HttpPost("auth/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest body, CancellationToken ct)
    {
        await _mediator.Send(new ResetPasswordCommand(body.Token, body.NewPassword), ct);
        return Ok(new { message = "Password reset successfully" });
    }

    // GET /api/user → usuário atual (se houver token, mesmo sem Authorize)
    [HttpGet("user")]
    public ActionResult<object> CurrentUser()
    {
        // Como não há Authorize, pode vir sem principal → 401
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { error = "Not authenticated" });

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
