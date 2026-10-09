using System;
using System.Collections.Generic;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class Account : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public Money Balance { get; private set; } = null!;
    public AccountType Type { get; private set; }

    public Guid UserId { get; private set; }
    public Guid? FamilyId { get; private set; }

    private readonly List<Transaction> _transactions = new();

    public IReadOnlyCollection<Transaction> Transactions =>
        _transactions.AsReadOnly();

    // Utilizado pelo EF Core durante a materialização.
    private Account()
    {
    }

    private Account(
        string name,
        Money initialBalance,
        AccountType type,
        Guid userId,
        Guid? familyId,
        string? description)
    {
        Name = name;
        Balance = initialBalance;
        Type = type;
        UserId = userId;
        FamilyId = familyId;
        Description = description;
    }

    public static Result<Account> Create(
        string name,
        Money initialBalance,
        AccountType type,
        Guid userId,
        Guid? familyId = null,
        string? description = null)
    {
        var detailsValidation = ValidateDetails(name, type);

        if (detailsValidation.IsFailure)
            return detailsValidation.Error;

        if (initialBalance is null)
            return Error.Validation("O saldo inicial é obrigatório.");

        if (userId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        if (familyId == Guid.Empty)
            return Error.Validation("O identificador da família é inválido.");

        return new Account(
            name.Trim(),
            initialBalance,
            type,
            userId,
            familyId,
            NormalizeDescription(description));
    }

    public Result Deposit(Money amount)
    {
        var validation = ValidateMovement(amount);

        if (validation.IsFailure)
            return validation;

        var balanceResult = Balance.TryAdd(amount);

        if (balanceResult.IsFailure)
            return balanceResult.Error;

        Balance = balanceResult.Value;
        Touch();

        return Result.Success();
    }

    public Result Withdraw(Money amount)
    {
        var validation = ValidateMovement(amount);

        if (validation.IsFailure)
            return validation;

        if (amount.Amount > Balance.Amount)
            return DomainErrors.Account.InsufficientFunds;

        var balanceResult = Balance.TrySubtract(amount);

        if (balanceResult.IsFailure)
            return balanceResult.Error;

        Balance = balanceResult.Value;
        Touch();

        return Result.Success();
    }

    public Result TransferTo(Account targetAccount, Money amount)
    {
        if (targetAccount is null)
            return Error.Validation("A conta de destino é obrigatória.");

        if (targetAccount.Id == Id)
        {
            return Error.Validation(
                "A conta de origem e a conta de destino devem ser diferentes.");
        }

        var sourceValidation = ValidateMovement(amount);

        if (sourceValidation.IsFailure)
            return sourceValidation;

        var targetValidation = targetAccount.ValidateMovement(amount);

        if (targetValidation.IsFailure)
            return targetValidation;

        if (amount.Amount > Balance.Amount)
            return DomainErrors.Account.InsufficientFunds;

        var sourceBalanceResult = Balance.TrySubtract(amount);

        if (sourceBalanceResult.IsFailure)
            return sourceBalanceResult.Error;

        var targetBalanceResult = targetAccount.Balance.TryAdd(amount);

        if (targetBalanceResult.IsFailure)
            return targetBalanceResult.Error;

        // Só alteramos as contas depois de validar os dois resultados.
        Balance = sourceBalanceResult.Value;
        targetAccount.Balance = targetBalanceResult.Value;

        Touch();
        targetAccount.Touch();

        return Result.Success();
    }

    public Result UpdateDetails(string name, AccountType type)
    {
        if (!IsActive)
            return DomainErrors.Account.Inactive;

        var validation = ValidateDetails(name, type);

        if (validation.IsFailure)
            return validation;

        Name = name.Trim();
        Type = type;
        Touch();

        return Result.Success();
    }

    public Result UpdateDescription(string? description)
    {
        if (!IsActive)
            return DomainErrors.Account.Inactive;

        Description = NormalizeDescription(description);
        Touch();

        return Result.Success();
    }

    public Result AssignToFamily(Guid familyId)
    {
        if (!IsActive)
            return DomainErrors.Account.Inactive;

        if (familyId == Guid.Empty)
            return Error.Validation("A família é obrigatória.");

        if (FamilyId == familyId)
            return Result.Success();

        FamilyId = familyId;
        Touch();

        return Result.Success();
    }

    private Result ValidateMovement(Money? amount)
    {
        if (!IsActive)
            return DomainErrors.Account.Inactive;

        if (amount is null || amount.Amount <= 0)
        {
            return Error.Validation(
                "O valor do movimento deve ser superior a zero.");
        }

        if (amount.Currency != Balance.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return Result.Success();
    }

    private static Result ValidateDetails(
        string name,
        AccountType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("O nome da conta é obrigatório.");

        if (!Enum.IsDefined(type) || type == AccountType.NotSpecified)
            return Error.Validation("O tipo de conta é inválido.");

        return Result.Success();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}