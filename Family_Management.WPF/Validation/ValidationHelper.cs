using System.Net.Mail;

namespace Family_Management.WPF.Validation;

public static class ValidationHelper
{
    public const int MinimumPasswordLength = 8;

    public static bool IsRequired(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalizedEmail = email.Trim();

        return MailAddress.TryCreate(
                   normalizedEmail,
                   out var address)
               && string.Equals(
                   address.Address,
                   normalizedEmail,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidPassword(string? password)
    {
        return !string.IsNullOrWhiteSpace(password)
               && password.Length >= MinimumPasswordLength;
    }

    public static bool IsPositiveAmount(decimal amount)
    {
        return amount > 0;
    }
}