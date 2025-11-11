using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;

    public JwtTokenService(IOptions<JwtOptions> options) => _opt = options.Value;

    public string GenerateAccessToken(User user, DateTimeOffset now)
    {
        var role = user.Role.ToShortName();

        var claims = new List<Claim>
        {
            // compat: NameIdentifier e "sub"
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, ToUnixTimeSeconds(now).ToString(), ClaimValueTypes.Integer64),

            new Claim(ClaimTypes.Email, user.Email.Value),
            new Claim("email", user.Email.Value),

            new Claim(ClaimTypes.Name, user.Name.Value),

            // role em ambos para compat
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),

            new Claim("email_confirmed", user.EmailConfirmed.ToString().ToLowerInvariant())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(_opt.AccessTokenMinutes).UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static long ToUnixTimeSeconds(DateTimeOffset dt) => dt.ToUnixTimeSeconds();
}
