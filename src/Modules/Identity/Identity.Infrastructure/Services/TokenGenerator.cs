using Identity.Contracts;
using Identity.Contracts.DTOs;
using Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Services;

public sealed class TokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _options;

    public TokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(TokenGenerationDto details)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SecretKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new List<Claim>
        {
            new(
                IdentityClaimTypes.UserId,
                details.UserId.ToString()
            ),

            new(
                IdentityClaimTypes.TokenId,
                Guid.NewGuid().ToString()
            ),

            new(
                IdentityClaimTypes.OrganizationId,
                details.OrganizationId.ToString()
            ),

            new(
                IdentityClaimTypes.Role,
                details.Role
            )
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                _options.DurationInHours
            ),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class IdentityClaimTypes
{
    public const string UserId = "sub";
    public const string TokenId = "jti";
    public const string OrganizationId = "org_id";
    public const string Role = "role";
}
