using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TitoRegistry.Application.Common.Interfaces;
using TitoRegistry.Domain.Entities;

namespace TitoRegistry.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime Expiration) GenerateToken(User user)
    {
        var secret = _configuration["JwtSettings:Secret"] 
            ?? "TransporteTitoSuperSecretKeyForJwtAuthenticationEnterprise2026!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "TitoRegistryApi";
        var audience = _configuration["JwtSettings:Audience"] ?? "TitoRegistryClient";
        var expirationDays = int.TryParse(_configuration["JwtSettings:ExpirationDays"], out int days) ? days : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("code", user.Code),
            new("isSuperAdmin", (user.Role == TitoRegistry.Domain.Enums.UserRole.SuperAdmin).ToString().ToLowerInvariant()),
            new("commissionRate", user.CommissionRate.ToString())
        };

        var expiration = DateTime.UtcNow.AddDays(expirationDays);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiration);
    }
}
