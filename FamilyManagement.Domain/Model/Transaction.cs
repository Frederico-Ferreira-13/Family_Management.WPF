using System;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class Transaction : BaseEntity
{
    public DateTime Date { get; private set; }

    public Money Amount { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public TransactionType Type { get; private set; }

    public bool IsConfirmed { get; private set; }
    public bool IsRecurringGenerated { get; private set; }

    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }

    public Guid? CategoryId { get; private set; }
    public Guid? TargetAccountId { get; private set; }
    public Guid? RecurringTransactionId { get; private set; }
    public Guid? BudgetId { get; private set; }
    public Guid? GoalId { get; private set; }

    public virtual User? User { get; private set; }
    public virtual Account? Account { get; private set; }
    public virtual Category? Category { get; private set; }
    public virtual Account? TargetAccount { get; private set; }

    public virtual RecurringTransaction? RecurringTransaction
    {
        get;
        private set;
    }

    public virtual Budget? Budget { get; private set; }
    public virtual Goal? Goal { get; private set; }

    private Transaction()
    {
    }

    private Transaction(
        DateTime date,
        Money amount,
        string description,
        TransactionType type,
        Guid userId,
        Guid accountId,
        Guid? categoryId,
        Guid? targetAccountId,
        string? notes,
        Guid? budgetId,
        Guid? goalId,
        Guid? recurringTransactionId,
        bool isConfirmed)
    {
        Date = NormalizeDate(date);

        Amount = amount;
        Description = description.Trim();
        Notes = NormalizeNotes(notes);

        Type = type;

        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        TargetAccountId = targetAccountId;
        BudgetId = budgetId;
        GoalId = goalId;
        RecurringTransactionId = recurringTransactionId;

        IsConfirmed = isConfirmed;
        IsRecurringGenerated = recurringTransactionId.HasValue;
    }

    public static Result<Transaction> CreateIncomeOrExpense(
        DateTime date,
        Money amount,
        string description,
        TransactionType type,
        Guid userId,
        Guid accountId,
        Guid categoryId,
        string? notes = null,
        Guid? budgetId = null,
        Guid? goalId = null,
        bool isConfirmed = true,
        bool isRecurringGenerated = false,
        DateTime? referenceDate = null)
    {
        var validation = ValidateBase(
            date,
            amount,
            description,
            userId,
            accountId,
            isConfirmed,
            referenceDate);

        if (validation.IsFailure)
            return validation.Error;

        if (type is not (
            TransactionType.Income or TransactionType.Expense))
        {
            return Error.Validation(
                "O movimento deve ser uma receita ou uma despesa.");
        }

        if (categoryId == Guid.Empty)
            return Error.Validation("A categoria é obrigatória.");

        if (budgetId.HasValue &&
            budgetId.Value == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do orçamento é inválido.");
        }

        if (goalId.HasValue &&
            goalId.Value == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da meta é inválido.");
        }

        if (isRecurringGenerated)
        {
            return Error.Validation(
                "Utilize CreateFromRecurring para criar um movimento de uma recorrência.");
        }

        return new Transaction(
            date,
            amount,
            description,
            type,
            userId,
            accountId,
            categoryId: categoryId,
            targetAccountId: null,
            notes: notes,
            budgetId: budgetId,
            goalId: goalId,
            recurringTransactionId: null,
            isConfirmed: isConfirmed);
    }

    public static Result<Transaction> CreateTransfer(
        DateTime date,
        Money amount,
        string description,
        Guid userId,
        Guid sourceAccountId,
        Guid targetAccountId,
        string? notes = null,
        bool isConfirmed = true,
        DateTime? referenceDate = null)
    {
        var validation = ValidateBase(
            date,
            amount,
            description,
            userId,
            sourceAccountId,
            isConfirmed,
            referenceDate);

        if (validation.IsFailure)
            return validation.Error;

        if (targetAccountId == Guid.Empty)
        {
            return Error.Validation(
                "A conta de destino é obrigatória.");
        }

        if (sourceAccountId == targetAccountId)
        {
            return Error.Validation(
                "As contas de origem e destino devem ser diferentes.");
        }

        return new Transaction(
            date,
            amount,
            description,
            TransactionType.Transfer,
            userId,
            sourceAccountId,
            categoryId: null,
            targetAccountId: targetAccountId,
            notes: notes,
            budgetId: null,
            goalId: null,
            recurringTransactionId: null,
            isConfirmed: isConfirmed);
    }

    public static Result<Transaction> CreateFromRecurring(
        DateTime date,
        Money amount,
        string description,
        TransactionType type,
        Guid userId,
        Guid accountId,
        Guid categoryId,
        Guid recurringTransactionId,
        bool isConfirmed = false,
        DateTime? referenceDate = null)
    {
        var validation = ValidateBase(
            date,
            amount,
            description,
            userId,
            accountId,
            isConfirmed,
            referenceDate);

        if (validation.IsFailure)
            return validation.Error;

        if (type is not (
            TransactionType.Income or TransactionType.Expense))
        {
            return Error.Validation(
                "A recorrência deve gerar uma receita ou uma despesa.");
        }

        if (categoryId == Guid.Empty)
            return Error.Validation("A categoria é obrigatória.");

        if (recurringTransactionId == Guid.Empty)
        {
            return Error.Validation(
                "A identificação da recorrência é obrigatória.");
        }

        return new Transaction(
            date,
            amount,
            description,
            type,
            userId,
            accountId,
            categoryId: categoryId,
            targetAccountId: null,
            notes: null,
            budgetId: null,
            goalId: null,
            recurringTransactionId: recurringTransactionId,
            isConfirmed: isConfirmed);
    }

    public Result Confirm(DateTime? referenceDate = null)
    {
        if (!IsActive)
            return InactiveError();

        if (IsConfirmed)
            return Result.Success();

        var today = NormalizeDate(
            referenceDate ?? DateTime.Today);

        if (Date > today)
        {
            return Error.BusinessRule(
                "transaction.future_confirmation",
                "Não é possível confirmar um movimento com data futura.");
        }

        IsConfirmed = true;
        Touch();

        return Result.Success();
    }

    public Result Unconfirm()
    {
        if (!IsActive)
            return InactiveError();

        if (!IsConfirmed)
            return Result.Success();

        IsConfirmed = false;
        Touch();

        return Result.Success();
    }

    public Result UpdateNotes(string? notes)
    {
        if (!IsActive)
            return InactiveError();

        var normalizedNotes = NormalizeNotes(notes);

        if (Notes == normalizedNotes)
            return Result.Success();

        Notes = normalizedNotes;
        Touch();

        return Result.Success();
    }

    public Result UpdateDetails(
        string? description,
        string? notes,
        Guid? categoryId,
        Money? amount,
        Guid? accountId,
        bool? isConfirmed,
        DateTime? referenceDate = null)
    {
        if (!IsActive)
            return InactiveError();

        if (description is not null &&
            string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation(
                "A descrição não pode estar vazia.");
        }

        if (amount is not null)
        {
            if (amount.Amount <= 0)
                return DomainErrors.Transaction.InvalidAmount;

            if (amount.Currency != Amount.Currency)
                return DomainErrors.Money.CurrencyMismatch;
        }

        if (accountId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da conta é inválido.");
        }

        if (Type == TransactionType.Transfer)
        {
            if (categoryId.HasValue)
            {
                return Error.Validation(
                    "Uma transferência não utiliza uma categoria de receita ou despesa.");
            }

            var resultingAccountId = accountId ?? AccountId;

            if (resultingAccountId == TargetAccountId)
            {
                return Error.Validation(
                    "As contas de origem e destino devem ser diferentes.");
            }
        }
        else if (categoryId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da categoria é inválido.");
        }

        var resultingConfirmation = isConfirmed ?? IsConfirmed;

        var today = NormalizeDate(
            referenceDate ?? DateTime.Today);

        if (resultingConfirmation && Date > today)
        {
            return Error.BusinessRule(
                "transaction.future_confirmation",
                "Não é possível confirmar um movimento com data futura.");
        }

        // A partir daqui, todas as validações locais passaram.

        if (description is not null)
            Description = description.Trim();

        if (amount is not null)
            Amount = amount;

        if (accountId.HasValue)
            AccountId = accountId.Value;

        if (Type != TransactionType.Transfer &&
            categoryId.HasValue)
        {
            CategoryId = categoryId.Value;
        }

        Notes = NormalizeNotes(notes);
        IsConfirmed = resultingConfirmation;

        Touch();

        return Result.Success();
    }

    private static Result ValidateBase(
        DateTime date,
        Money? amount,
        string description,
        Guid userId,
        Guid accountId,
        bool isConfirmed,
        DateTime? referenceDate)
    {
        if (date == default)
            return Error.Validation("A data é obrigatória.");

        if (amount is null || amount.Amount <= 0)
            return DomainErrors.Transaction.InvalidAmount;

        if (string.IsNullOrWhiteSpace(description))
            return Error.Validation("A descrição é obrigatória.");

        if (userId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        if (accountId == Guid.Empty)
            return Error.Validation("A conta é obrigatória.");

        var transactionDate = NormalizeDate(date);

        var today = NormalizeDate(
            referenceDate ?? DateTime.Today);

        if (isConfirmed && transactionDate > today)
        {
            return Error.BusinessRule(
                "transaction.future_confirmation",
                "Um movimento futuro deve permanecer por confirmar.");
        }

        return Result.Success();
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        return DateTime.SpecifyKind(
            date.Date,
            DateTimeKind.Unspecified);
    }

    private static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
    }

    private static Error InactiveError()
    {
        return Error.Conflict(
            "transaction.inactive",
            "A transação está inativa.");
    }
}