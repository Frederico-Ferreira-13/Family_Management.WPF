namespace FamilyManagement.Application.Common.Models;

public sealed class TokenResponse
{
    public string Token { get; init; } = string.Empty;

    public DateTime Expiration { get; init; }

    public string Email { get; init; } = string.Empty;
}