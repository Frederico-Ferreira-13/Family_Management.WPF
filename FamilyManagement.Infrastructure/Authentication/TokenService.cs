using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.Common.Models;
using FamilyManagement.Application.UseCases.Users.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace FamilyManagement.Infrastructure.Authentication;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings
            ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public TokenResponse GenerateToken(UserDTO user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Email,
                user.Email),

            new(
                ClaimTypes.Name,
                user.UserName)
        };

        if (user.IsAdmin)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    "Admin"));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _jwtSettings.SecurityKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha512Signature);

        var expiresAt = DateTime.UtcNow.AddMinutes(
            _jwtSettings.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = credentials,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience
        };

        var handler = new JwtSecurityTokenHandler();

        var token = handler.CreateToken(descriptor);

        return new TokenResponse
        {
            Token = handler.WriteToken(token),
            Expiration = expiresAt,
            Email = user.Email
        };
    }
}