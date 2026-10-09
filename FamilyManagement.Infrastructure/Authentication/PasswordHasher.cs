using FamilyManagement.Application.Common.Interfaces;

namespace FamilyManagement.Infrastructure.Authentication;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "A password não pode ser nula ou vazia.",
                nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(
            password,
            WorkFactor);
    }

    public bool VerifyPassword(
        string password,
        string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(
                password,
                hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}