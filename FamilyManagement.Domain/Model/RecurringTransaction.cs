using System;
using System.Collections.Generic;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class RecurringTransaction : BaseEntity
{
    public string Description { get; private set; } = string.Empty;

    public Money Amount { get; private set; } = null!;

    public TransactionType Type { get; private set; }
    public Frequency Frequency { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    public DateTime? LastGenerateDate { get; private set; }
    public DateTime NextDueDate { get; private set; }

    public Guid CategoryId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid UserId { get; private set; }

    public virtual Category? Category { get; private set; }
    public virtual Account? Account { get; private set; }
    public virtual User? User { get; private set; }

    private readonly List<Transaction> _generatedTransactions = new();

    public virtual IReadOnlyCollection<Transaction>
        GeneratedTransactions =>
            _generatedTransactions.AsReadOnly();

    private RecurringTransaction()
    {
    }

    private RecurringTransaction(
        string description,
        Money amount,
        TransactionType type,
        Frequency frequency,
        DateTime startDate,
        Guid categoryId,
        Guid accountId,
        Guid userId,
        DateTime? endDate)
    {
        Description = description.Trim();
        Amount = amount;

        Type = type;
        Frequency = frequency;

        StartDate = NormalizeDate(startDate);

        EndDate = endDate.HasValue
            ? NormalizeDate(endDate.Value)
            : null;

        NextDueDate = StartDate;

        CategoryId = categoryId;
        AccountId = accountId;
        UserId = userId;
    }

    public static Result<RecurringTransaction> Create(
        string description,
        Money amount,
        TransactionType type,
        Frequency frequency,
        DateTime startDate,
        Guid categoryId,
        Guid accountId,
        Guid userId,
        DateTime? endDate = null)
    {
        var validation = ValidateDetails(
            description,
            amount,
            type,
            frequency,
            startDate,
            categoryId,
            accountId,
            endDate);

        if (validation.IsFailure)
            return validation.Error;

        if (userId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        return new RecurringTransaction(
            description,
            amount,
            type,
            frequency,
            startDate,
            categoryId,
            accountId,
            userId,
            endDate);
    }

    public Result<Transaction> GenerateTransaction(
        bool isConfirmed = false,
        DateTime? referenceDate = null)
    {
        var validation = ValidatePendingOccurrence(referenceDate);

        if (validation.IsFailure)
            return validation.Error;

        // Esta operação não altera NextDueDate nem LastGenerateDate.
        return Transaction.CreateFromRecurring(
            date: NextDueDate,
            amount: Amount,
            description: Description,
            type: Type,
            userId: UserId,
            accountId: AccountId,
            categoryId: CategoryId,
            recurringTransactionId: Id,
            isConfirmed: isConfirmed,
            referenceDate: referenceDate);
    }

    public Result RecordGeneratedTransaction(
        Transaction transaction,
        DateTime? referenceDate = null)
    {
        var pendingValidation =
            ValidatePendingOccurrence(referenceDate);

        if (pendingValidation.IsFailure)
            return pendingValidation;

        if (transaction is null || !transaction.IsActive)
        {
            return Error.Validation(
                "A transação deve existir e estar ativa.");
        }

        if (!transaction.IsRecurringGenerated ||
            transaction.RecurringTransactionId != Id)
        {
            return Error.Validation(
                "A transação não pertence a esta recorrência.");
        }

        if (transaction.Date != NextDueDate ||
            transaction.UserId != UserId ||
            transaction.AccountId != AccountId ||
            transaction.CategoryId != CategoryId ||
            transaction.Type != Type ||
            transaction.Amount != Amount)
        {
            return Error.Validation(
                "A transação não corresponde à ocorrência pendente.");
        }

        DateTime nextDueDate;

        try
        {
            nextDueDate = CalculateNextDueDate();
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.BusinessRule(
                "recurring.date_limit",
                "A próxima ocorrência ultrapassa o calendário suportado.");
        }

        // Só alteramos o estado depois de concluir as validações.
        _generatedTransactions.Add(transaction);

        LastGenerateDate = NextDueDate;
        NextDueDate = nextDueDate;

        if (EndDate.HasValue &&
            NextDueDate > EndDate.Value)
        {
            IsActive = false;
        }

        Touch();

        return Result.Success();
    }

    public Result UpdateDetails(
        string description,
        Money amount,
        TransactionType type,
        Frequency frequency,
        Guid categoryId,
        Guid accountId,
        DateTime? endDate,
        bool isActive)
    {
        var validation = ValidateDetails(
            description,
            amount,
            type,
            frequency,
            StartDate,
            categoryId,
            accountId,
            endDate);

        if (validation.IsFailure)
            return validation;

        if (amount.Currency != Amount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        if (LastGenerateDate.HasValue &&
            frequency != Frequency)
        {
            return Error.BusinessRule(
                "recurring.frequency_already_used",
                "Crie uma nova recorrência para alterar uma frequência que já gerou movimentos.");
        }

        var normalizedEndDate = endDate.HasValue
            ? NormalizeDate(endDate.Value)
            : (DateTime?)null;

        if (isActive &&
            normalizedEndDate.HasValue &&
            NextDueDate > normalizedEndDate.Value)
        {
            return Error.BusinessRule(
                "recurring.end_before_next_occurrence",
                "A data de fim é anterior à próxima ocorrência.");
        }

        Description = description.Trim();
        Amount = amount;

        Type = type;
        Frequency = frequency;

        CategoryId = categoryId;
        AccountId = accountId;

        EndDate = normalizedEndDate;
        IsActive = isActive;

        Touch();

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
            return Result.Success();

        if (EndDate.HasValue &&
            NextDueDate > EndDate.Value)
        {
            return Error.BusinessRule(
                "recurring.finished",
                "A recorrência terminou. Altere a data de fim antes de a reativar.");
        }

        base.Activate();

        return Result.Success();
    }

    public override void Activate()
    {
        var result = Reactivate();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }
    }

    private Result ValidatePendingOccurrence(
        DateTime? referenceDate)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "recurring.inactive",
                "A recorrência está inativa.");
        }

        var today = NormalizeDate(
            referenceDate ?? DateTime.Today);

        if (NextDueDate > today)
        {
            return Error.BusinessRule(
                "recurring.not_due",
                "A próxima ocorrência ainda não venceu.");
        }

        if (EndDate.HasValue &&
            NextDueDate > EndDate.Value)
        {
            return Error.BusinessRule(
                "recurring.finished",
                "A recorrência terminou.");
        }

        if (LastGenerateDate.HasValue &&
            NextDueDate <= LastGenerateDate.Value)
        {
            return Error.BusinessRule(
                "recurring.invalid_schedule",
                "A agenda não avançou para uma nova ocorrência.");
        }

        return Result.Success();
    }

    private DateTime CalculateNextDueDate()
    {
        if (Frequency == Frequency.Daily)
            return NextDueDate.AddDays(1);

        if (Frequency == Frequency.Weekly)
            return NextDueDate.AddDays(7);

        if (Frequency == Frequency.Biweekly)
            return NextDueDate.AddDays(14);

        var monthsToAdd = Frequency switch
        {
            Frequency.Monthly => 1,
            Frequency.Quarterly => 3,
            Frequency.Semiannually => 6,
            Frequency.Annually => 12,

            _ => throw new InvalidOperationException(
                "A frequência da recorrência é inválida.")
        };

        var elapsedMonths =
            (NextDueDate.Year - StartDate.Year) * 12
            + NextDueDate.Month
            - StartDate.Month;

        // Parte sempre da data inicial, preservando o dia original.
        // Exemplo: 31/janeiro -> 28/fevereiro -> 31/março.
        return StartDate.AddMonths(
            elapsedMonths + monthsToAdd);
    }

    private static Result ValidateDetails(
        string description,
        Money? amount,
        TransactionType type,
        Frequency frequency,
        DateTime startDate,
        Guid categoryId,
        Guid accountId,
        DateTime? endDate)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation(
                "A descrição é obrigatória.");
        }

        if (amount is null || amount.Amount <= 0)
            return DomainErrors.Transaction.InvalidAmount;

        if (type is not (
            TransactionType.Income or TransactionType.Expense))
        {
            return Error.Validation(
                "A recorrência deve ser uma receita ou uma despesa.");
        }

        if (!Enum.IsDefined(frequency))
        {
            return Error.Validation(
                "A frequência é inválida.");
        }

        if (startDate == default)
        {
            return Error.Validation(
                "A data de início é obrigatória.");
        }

        if (endDate.HasValue &&
            NormalizeDate(endDate.Value) <
            NormalizeDate(startDate))
        {
            return Error.Validation(
                "A data de fim não pode ser anterior à data de início.");
        }

        if (categoryId == Guid.Empty)
            return Error.Validation("A categoria é obrigatória.");

        if (accountId == Guid.Empty)
            return Error.Validation("A conta é obrigatória.");

        return Result.Success();
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        return DateTime.SpecifyKind(
            date.Date,
            DateTimeKind.Unspecified);
    }
}