using System;
using System.Collections.Generic;

namespace FamilyManagement.Domain.Errors;

public readonly record struct Error
{
    private readonly string? _code;
    private readonly string? _message;

    public ErrorType Type { get; }

    public string Code => _code ?? string.Empty;
    public string Message => _message ?? string.Empty;

    public IReadOnlyDictionary<string, string[]>? ValidationErrors
    {
        get;
    }

    public bool IsNone => Type == ErrorType.None;

    public static Error None => default;

    private Error(
        ErrorType type,
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        if (!Enum.IsDefined(type) || type == ErrorType.None)
        {
            throw new ArgumentException(
                "O tipo deve representar um erro válido.",
                nameof(type));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "O código do erro é obrigatório.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "A mensagem do erro é obrigatória.",
                nameof(message));
        }

        Type = type;
        _code = code.Trim();
        _message = message.Trim();
        ValidationErrors = validationErrors;
    }

    public static Error NotFound(string code, string message)
    {
        return new Error(
            ErrorType.NotFound,
            code,
            message);
    }

    public static Error Validation(
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        return new Error(
            ErrorType.Validation,
            "ValidationError",
            message,
            errors);
    }

    public static Error Conflict(string code, string message)
    {
        return new Error(
            ErrorType.Conflict,
            code,
            message);
    }

    public static Error BusinessRule(string code, string message)
    {
        return new Error(
            ErrorType.BusinessRuleViolation,
            code,
            message);
    }

    public static Error Unauthorized(string code, string message)
    {
        return new Error(
            ErrorType.Unauthorized,
            code,
            message);
    }

    public static Error Failure(string code, string message)
    {
        return new Error(
            ErrorType.Internal,
            code,
            message);
    }
}