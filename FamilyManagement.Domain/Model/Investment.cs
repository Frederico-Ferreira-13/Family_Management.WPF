using System;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class Investment : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public InvestmentType Type { get; private set; }

    public Money InitialValue { get; private set; } = null!;
    public Money CurrentValue { get; private set; } = null!;

    public DateTime PurchaseDate { get; private set; }
    public DateTime? LastUpdateDate { get; private set; }

    public Guid UserId { get; private set; }
    public Guid? AccountId { get; private set; }

    public virtual User? User { get; private set; }
    public virtual Account? Account { get; private set; }

    // A diferença pode ser negativa.
    // A moeda é a mesma de InitialValue e CurrentValue.
    public decimal ProfitLoss =>
        CurrentValue.Amount - InitialValue.Amount;

    public double ProfitLossPercentage =>
        InitialValue.Amount == 0
            ? 0
            : ((double)ProfitLoss / (double)InitialValue.Amount) * 100;

    public bool IsProfitable => ProfitLoss > 0;

    private Investment()
    {
    }

    private Investment(
        string name,
        InvestmentType type,
        Money initialValue,
        DateTime purchaseDateUtc,
        Guid userId,
        Guid? accountId)
    {
        Name = name;
        Type = type;

        InitialValue = initialValue;
        CurrentValue = initialValue;

        PurchaseDate = purchaseDateUtc;
        LastUpdateDate = DateTime.UtcNow;

        UserId = userId;
        AccountId = accountId;
    }

    public static Result<Investment> Create(
        string name,
        InvestmentType type,
        Money initialValue,
        DateTime purchaseDate,
        Guid userId,
        Guid? accountId = null)
    {
        var detailsValidation = ValidateDetails(
            name,
            type,
            accountId);

        if (detailsValidation.IsFailure)
            return detailsValidation.Error;

        if (initialValue is null)
            return Error.Validation("O valor inicial é obrigatório.");

        if (userId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        if (purchaseDate == default)
            return Error.Validation("A data de compra é obrigatória.");

        var purchaseDateUtc = purchaseDate.ToUniversalTime();

        if (purchaseDateUtc.Date > DateTime.UtcNow.Date)
        {
            return Error.Validation(
                "A data de compra não pode estar no futuro.");
        }

        return new Investment(
            name.Trim(),
            type,
            initialValue,
            purchaseDateUtc,
            userId,
            accountId);
    }

    public Result UpdateCurrentValue(Money newValue)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "investment.inactive",
                "O investimento está inativo.");
        }

        if (newValue is null)
            return Error.Validation("O valor atual é obrigatório.");

        if (newValue.Currency != InitialValue.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        CurrentValue = newValue;
        LastUpdateDate = DateTime.UtcNow;

        Touch();

        return Result.Success();
    }

    public Result UpdateDetails(
        string name,
        InvestmentType type,
        Guid? accountId)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "investment.inactive",
                "O investimento está inativo.");
        }

        var validation = ValidateDetails(
            name,
            type,
            accountId);

        if (validation.IsFailure)
            return validation;

        Name = name.Trim();
        Type = type;
        AccountId = accountId;

        Touch();

        return Result.Success();
    }

    public Result DeactivateInvestment()
    {
        if (!IsActive)
            return Result.Success();

        if (CurrentValue.Amount > 0)
        {
            return Error.BusinessRule(
                "investment.has_value",
                "Não é possível desativar um investimento com valor atual superior a zero.");
        }

        base.Deactivate();

        return Result.Success();
    }

    public override void Deactivate()
    {
        var result = DeactivateInvestment();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }
    }

    private static Result ValidateDetails(
        string name,
        InvestmentType type,
        Guid? accountId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("O nome do investimento é obrigatório.");

        if (!Enum.IsDefined(type) ||
            type == InvestmentType.NotSpecified)
        {
            return Error.Validation(
                "O tipo de investimento é inválido.");
        }

        if (accountId == Guid.Empty)
            return Error.Validation("O identificador da conta é inválido.");

        return Result.Success();
    }
}