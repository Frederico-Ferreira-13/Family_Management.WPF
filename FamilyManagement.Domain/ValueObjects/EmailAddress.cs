using System;
using System.Linq;
using System.Net.Mail;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation(
                "O email é obrigatório.");
        }

        var normalizedEmail = email.Trim();

        if (!IsValidEmail(normalizedEmail))
        {
            return Error.Validation(
                "O formato do email é inválido.");
        }

        return new EmailAddress(
            normalizedEmail.ToLowerInvariant());
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length > 254)
            return false;

        if (email.Any(char.IsWhiteSpace))
            return false;

        if (!MailAddress.TryCreate(email, out var parsedEmail))
            return false;

        if (!string.Equals(
                parsedEmail.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = parsedEmail.Host;

        if (!host.Contains('.'))
            return false;

        if (host.StartsWith('.') ||
            host.EndsWith('.') ||
            host.Contains(".."))
        {
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return email.Value;
    }
}