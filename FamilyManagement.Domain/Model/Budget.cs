using System;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class Budget : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public Money BudgetedAmount { get; private set; } = null!;
    public Money CurrentSpent { get; private set; } = null!;

    public int Month { get; private set; }
    public int Year { get; private set; }

    public Guid CategoryId { get; private set; }
    public Guid UserId { get; private set; }

    public decimal RemainingAmount =>
        BudgetedAmount.Amount - CurrentSpent.Amount;

    public bool IsOverBudget =>
        CurrentSpent.Amount > BudgetedAmount.Amount;

    private Budget()
    {
    }

    private Budget(
        string name,
        Money amount,
        int month,
        int year,
        Guid categoryId,
        Guid userId)
    {
        Name = name;
        BudgetedAmount = amount;
        CurrentSpent = Money.Create(0, amount.Currency);

        Month = month;
        Year = year;

        CategoryId = categoryId;
        UserId = userId;
    }

    public static Result<Budget> Create(
        string name,
        Money amount,
        int month,
        int year,
        Guid categoryId,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("O nome do orçamento é obrigatório.");

        if (amount is null)
            return Error.Validation("O valor do orçamento é obrigatório.");

        if (month is < 1 or > 12)
            return Error.Validation("O mês deve estar entre 1 e 12.");

        if (year is < 2000 or > 9999)
            return Error.Validation("O ano do orçamento é inválido.");

        if (categoryId == Guid.Empty)
            return Error.Validation("A categoria é obrigatória.");

        if (userId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        return new Budget(
            name.Trim(),
            amount,
            month,
            year,
            categoryId,
            userId);
    }

    public Result UpdateDetails(string name, Money budgetedAmount)
    {
        if (!IsActive)
            return Error.Conflict("budget.inactive", "O orçamento está inativo.");

        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("O nome do orçamento é obrigatório.");

        if (budgetedAmount is null)
            return Error.Validation("O valor do orçamento é obrigatório.");

        if (budgetedAmount.Currency != BudgetedAmount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        Name = name.Trim();
        BudgetedAmount = budgetedAmount;
        Touch();

        return Result.Success();
    }

    public Result AddExpense(Money amount)
    {
        var validation = ValidateMovement(amount);

        if (validation.IsFailure)
            return validation;

        var spentResult = CurrentSpent.TryAdd(amount);

        if (spentResult.IsFailure)
            return spentResult.Error;

        CurrentSpent = spentResult.Value;
        Touch();

        return Result.Success();
    }

    public Result RemoveExpense(Money amount)
    {
        var validation = ValidateMovement(amount);

        if (validation.IsFailure)
            return validation;

        if (amount.Amount > CurrentSpent.Amount)
        {
            return Error.BusinessRule(
                "budget.invalid_reversal",
                "O valor a reverter é superior à despesa acumulada.");
        }

        var spentResult = CurrentSpent.TrySubtract(amount);

        if (spentResult.IsFailure)
            return spentResult.Error;

        CurrentSpent = spentResult.Value;
        Touch();

        return Result.Success();
    }

    private Result ValidateMovement(Money? amount)
    {
        if (!IsActive)
            return Error.Conflict("budget.inactive", "O orçamento está inativo.");

        if (amount is null || amount.Amount <= 0)
        {
            return Error.Validation(
                "O valor da despesa deve ser superior a zero.");
        }

        if (amount.Currency != BudgetedAmount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return Result.Success();
    }
}