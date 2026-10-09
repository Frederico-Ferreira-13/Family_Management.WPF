using System;
using System.Linq;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> TryCreate(
        decimal amount,
        string? currency = "EUR")
    {
        if (amount < 0)
            return DomainErrors.Money.InvalidAmount;

        var normalizedCurrency = currency?
            .Trim()
            .ToUpperInvariant();

        if (normalizedCurrency is null ||
            normalizedCurrency.Length != 3 ||
            !normalizedCurrency.All(
                character => character is >= 'A' and <= 'Z'))
        {
            return DomainErrors.Money.InvalidCurrency;
        }

        return new Money(
            amount,
            normalizedCurrency);
    }

    public static Money Create(
        decimal amount,
        string currency = "EUR")
    {
        var result = TryCreate(amount, currency);

        if (result.IsFailure)
        {
            throw new ArgumentException(
                result.Error.Message);
        }

        return result.Value;
    }

    public Result<Money> TryAdd(Money? other)
    {
        if (other is null)
            return DomainErrors.Money.MissingAmount;

        if (Currency != other.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        if (other.Amount > decimal.MaxValue - Amount)
            return DomainErrors.Money.Overflow;

        return new Money(
            Amount + other.Amount,
            Currency);
    }

    public Result<Money> TrySubtract(Money? other)
    {
        if (other is null)
            return DomainErrors.Money.MissingAmount;

        if (Currency != other.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        if (other.Amount > Amount)
            return DomainErrors.Money.NegativeResult;

        return new Money(
            Amount - other.Amount,
            Currency);
    }

    public Money Add(Money other)
    {
        var result = TryAdd(other);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }

        return result.Value;
    }

    public Money Subtract(Money other)
    {
        var result = TrySubtract(other);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }

        return result.Value;
    }
}