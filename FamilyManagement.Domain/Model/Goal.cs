using System;
using System.Collections.Generic;
using System.Linq;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class Goal : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public Money TargetAmount { get; private set; } = null!;
    public Money CurrentAmount { get; private set; } = null!;

    public DateTime StartDate { get; private set; }
    public DateTime? TargetDate { get; private set; }

    public bool IsAchieved { get; private set; }

    public Guid UserId { get; private set; }
    public virtual User? User { get; private set; }

    private readonly List<Transaction> _contributions = new();

    public virtual IReadOnlyCollection<Transaction> Contributions =>
        _contributions.AsReadOnly();

    public Money RemainingAmount =>
        Money.Create(
            Math.Max(
                0,
                TargetAmount.Amount - CurrentAmount.Amount),
            TargetAmount.Currency);

    public double ProgressPercentage
    {
        get
        {
            if (TargetAmount.Amount <= 0)
                return 0;

            if (CurrentAmount.Amount >= TargetAmount.Amount)
                return 100;

            return (double)(
                CurrentAmount.Amount / TargetAmount.Amount) * 100;
        }
    }

    private Goal()
    {
    }

    private Goal(
        string name,
        Money targetAmount,
        Guid userId,
        DateTime? targetDate)
    {
        Name = name;
        TargetAmount = targetAmount;
        CurrentAmount = Money.Create(0, targetAmount.Currency);

        StartDate = DateTime.UtcNow;
        TargetDate = targetDate;

        UserId = userId;
        IsAchieved = false;
    }

    public static Result<Goal> Create(
        string name,
        Money targetAmount,
        Guid userId,
        DateTime? targetDate = null)
    {
        var validation = ValidateDetails(
            name,
            targetAmount,
            targetDate);

        if (validation.IsFailure)
            return validation.Error;

        if (userId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        return new Goal(
            name.Trim(),
            targetAmount,
            userId,
            targetDate);
    }

    public Result UpdateDetails(
        string name,
        Money targetAmount,
        DateTime? targetDate)
    {
        if (!IsActive)
            return Error.Conflict("goal.inactive", "A meta está inativa.");

        var validation = ValidateDetails(
            name,
            targetAmount,
            targetDate);

        if (validation.IsFailure)
            return validation;

        if (targetAmount.Currency != TargetAmount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        Name = name.Trim();
        TargetAmount = targetAmount;
        TargetDate = targetDate;

        UpdateStatus();
        Touch();

        return Result.Success();
    }

    public Result AddContribution(Transaction transaction)
    {
        if (!IsActive)
            return Error.Conflict("goal.inactive", "A meta está inativa.");

        if (transaction is null)
            return Error.Validation("A transação é obrigatória.");

        if (!transaction.IsActive || !transaction.IsConfirmed)
        {
            return Error.Validation(
                "A contribuição exige uma transação ativa e confirmada.");
        }

        if (transaction.GoalId != Id)
        {
            return Error.Validation(
                "A transação não está associada a esta meta.");
        }

        if (transaction.UserId != UserId)
        {
            return Error.Validation(
                "A transação pertence a outro utilizador.");
        }

        // A mesma transação não deve ser contabilizada duas vezes.
        if (_contributions.Any(item => item.Id == transaction.Id))
            return Result.Success();

        var depositResult = Deposit(transaction.Amount);

        if (depositResult.IsFailure)
            return depositResult;

        _contributions.Add(transaction);

        return Result.Success();
    }

    public Result Deposit(Money amount)
    {
        var validation = ValidateMovement(amount);

        if (validation.IsFailure)
            return validation;

        var amountResult = CurrentAmount.TryAdd(amount);

        if (amountResult.IsFailure)
            return amountResult.Error;

        CurrentAmount = amountResult.Value;

        UpdateStatus();
        Touch();

        return Result.Success();
    }

    public Result Withdraw(Money amount)
    {
        var validation = ValidateMovement(amount);

        if (validation.IsFailure)
            return validation;

        if (amount.Amount > CurrentAmount.Amount)
        {
            return Error.BusinessRule(
                "goal.insufficient_funds",
                "O valor a retirar é superior ao montante acumulado.");
        }

        var amountResult = CurrentAmount.TrySubtract(amount);

        if (amountResult.IsFailure)
            return amountResult.Error;

        CurrentAmount = amountResult.Value;

        UpdateStatus();
        Touch();

        return Result.Success();
    }

    private Result ValidateMovement(Money? amount)
    {
        if (!IsActive)
            return Error.Conflict("goal.inactive", "A meta está inativa.");

        if (amount is null || amount.Amount <= 0)
        {
            return Error.Validation(
                "O valor do movimento deve ser superior a zero.");
        }

        if (amount.Currency != TargetAmount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return Result.Success();
    }

    private static Result ValidateDetails(
        string name,
        Money? targetAmount,
        DateTime? targetDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("O nome da meta é obrigatório.");

        if (targetAmount is null || targetAmount.Amount <= 0)
        {
            return Error.Validation(
                "O valor objetivo deve ser superior a zero.");
        }

        if (targetDate.HasValue &&
            targetDate.Value.Date < DateTime.UtcNow.Date)
        {
            return Error.Validation(
                "A data objetivo não pode estar no passado.");
        }

        return Result.Success();
    }

    private void UpdateStatus()
    {
        IsAchieved = CurrentAmount.Amount >= TargetAmount.Amount;
    }
}