using System.Text;

namespace FamilyManagement.Application.Common.Models;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string SecurityKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 1440;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(SecurityKey)
            && Encoding.UTF8.GetByteCount(SecurityKey) >= 64
            && !string.IsNullOrWhiteSpace(Issuer)
            && !string.IsNullOrWhiteSpace(Audience)
            && ExpirationMinutes > 0;
    }
}