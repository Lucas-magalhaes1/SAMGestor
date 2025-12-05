using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces.Auth;

namespace SAMGestor.Infrastructure.Services;

public sealed class JwtTokenDecoder : IJwtTokenDecoder
{
    private readonly JwtOptions _opt;
    private readonly TokenValidationParameters _validationParams;

    public JwtTokenDecoder(IOptions<JwtOptions> options)
    {
        _opt = options.Value;
        
        _validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _opt.Issuer,
            ValidateAudience = true,
            ValidAudience = _opt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret)),
            ValidateLifetime = false, // ⚠ CRUCIAL: Não validar expiração
            ClockSkew = TimeSpan.Zero
        };
    }

    public Guid ExtractUserIdFromExpiredToken(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Validar assinatura mas permitir token expirado
            var principal = handler.ValidateToken(accessToken, _validationParams, out var validatedToken);

            // Verificar se é JWT válido
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token algorithm");
            }

            // Extrair userId (claim "sub" ou NameIdentifier)
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new SecurityTokenException("Token does not contain valid user ID");
            }

            return userId;
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Invalid access token", ex);
        }
    }
}
