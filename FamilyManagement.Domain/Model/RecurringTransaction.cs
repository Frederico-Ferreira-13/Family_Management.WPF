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

    public RecurringTransactionScope Scope { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public DateTime? LastGenerateDate { get; private set; }

    public DateTime NextDueDate { get; private set; }

    public DateTime? ScheduleAnchorDate { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid? AccountId { get; private set; }

    // Utilizador responsável pelo registo.
    public Guid UserId { get; private set; }

    // Membro da família que recebe o rendimento.
    public Guid? BeneficiaryUserId { get; private set; }

    public Guid? FinancialPlanId { get; private set; }

    public Guid? FamilyId { get; private set; }

    public virtual Family? Family { get; private set; }

    public virtual FinancialPlan? FinancialPlan { get; private set; }

    public virtual Category? Category { get; private set; }

    public virtual Account? Account { get; private set; }

    public virtual User? User { get; private set; }

    public virtual User? BeneficiaryUser { get; private set; }

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
        Guid? accountId,
        Guid? familyId,
        Guid userId,
        Guid? financialPlanId,
        DateTime? endDate,
        Guid? beneficiaryUserId)
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

        FinancialPlanId = financialPlanId;

        FamilyId = familyId;

        Scope = familyId.HasValue
            ? RecurringTransactionScope.Family
            : RecurringTransactionScope.Personal;

        BeneficiaryUserId = beneficiaryUserId;
    }

    public static Result<RecurringTransaction> Create(
        string description,
        Money amount,
        TransactionType type,
        Frequency frequency,
        DateTime startDate,
        Guid categoryId,
        Guid? accountId,
        Guid userId,
        Guid? familyId = null,
        Guid? financialPlanId = null,
        DateTime? endDate = null,
        Guid? beneficiaryUserId = null)
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
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (financialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do plano financeiro é inválido.");
        }

        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (!familyId.HasValue && financialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência pessoal não pode estar associada a um plano financeiro familiar.");
        }

        var beneficiaryValidation = ValidateBeneficiary(
            beneficiaryUserId,
            type,
            familyId);

        if (beneficiaryValidation.IsFailure)
            return beneficiaryValidation.Error;

        return new RecurringTransaction(
            description: description,
            amount: amount,
            type: type,
            frequency: frequency,
            startDate: startDate,
            categoryId: categoryId,
            accountId: accountId,
            familyId: familyId,
            userId: userId,
            financialPlanId: financialPlanId,
            endDate: endDate,
            beneficiaryUserId: beneficiaryUserId);
    }

    public Result AssignBeneficiary(Guid? beneficiaryUserId)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "recurring.inactive",
                "Não é possível alterar uma recorrência inativa.");
        }

        var validation = ValidateBeneficiary(
            beneficiaryUserId,
            Type,
            FamilyId);

        if (validation.IsFailure)
            return validation;

        if (BeneficiaryUserId == beneficiaryUserId)
            return Result.Success();

        BeneficiaryUserId = beneficiaryUserId;

        Touch();

        return Result.Success();
    }

    public Result<Transaction> GenerateTransaction(
        bool isConfirmed = false,
        DateTime? referenceDate = null)
    {
        var validation = ValidatePendingOccurrence(referenceDate);

        if (validation.IsFailure)
            return validation.Error;

        if (Scope == RecurringTransactionScope.Family &&
            !FamilyId.HasValue)
        {
            return Error.BusinessRule(
                "recurring.invalid_family_scope",
                "Uma recorrência familiar deve estar associada a uma família.");
        }

        if (Scope == RecurringTransactionScope.Personal &&
            (FamilyId.HasValue ||
            FinancialPlanId.HasValue ||
            BeneficiaryUserId.HasValue))
        {
            return Error.BusinessRule(
                "recurring.invalid_personal_scope",
                "Uma recorrência pessoal não pode ter associações financeiras familiares.");
        }

        return Transaction.CreateFromRecurring(
            date: NormalizeDate(NextDueDate),
            amount: Amount,
            description: Description,
            type: Type,
            userId: UserId,
            accountId: AccountId,
            categoryId: CategoryId,
            recurringTransactionId: Id,
            isConfirmed: isConfirmed,
            referenceDate: referenceDate,
            familyId: Scope == RecurringTransactionScope.Family
                ? FamilyId
                : null,
            financialPlanId: Scope == RecurringTransactionScope.Family
                ? FinancialPlanId
                : null,
            beneficiaryUserId: Scope == RecurringTransactionScope.Family
                ? BeneficiaryUserId
                : null);
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

        if (NormalizeDate(transaction.Date) != NormalizeDate(NextDueDate) ||
            transaction.UserId != UserId ||
            transaction.AccountId != AccountId ||
            transaction.CategoryId != CategoryId ||
            transaction.Type != Type ||
            transaction.FinancialPlanId != FinancialPlanId ||
            transaction.FamilyId != FamilyId ||
            transaction.Scope != Scope ||
            transaction.BeneficiaryUserId != BeneficiaryUserId ||
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

        _generatedTransactions.Add(transaction);

        LastGenerateDate = NormalizeDate(NextDueDate);

        NextDueDate = nextDueDate;

        if (EndDate.HasValue &&
            NormalizeDate(NextDueDate) > NormalizeDate(EndDate.Value))
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
        Guid? accountId,
        DateTime? endDate,
        bool isActive,
        DateTime? scheduleDate = null)
    {
        var currentStart = NormalizeDate(StartDate);
        var currentNext = NormalizeDate(NextDueDate);

        var requestedDate = scheduleDate.HasValue
            ? NormalizeDate(scheduleDate.Value)
            : LastGenerateDate.HasValue
                ? currentNext
                : currentStart;

        if (requestedDate == default)
        {
            return Error.Validation(
                "A data do agendamento é obrigatória.");
        }

        var hasGeneratedTransactions = LastGenerateDate.HasValue;

        var scheduleChanged = requestedDate !=
            (hasGeneratedTransactions ? currentNext : currentStart);

        var resultingStart = hasGeneratedTransactions
            ? currentStart
            : requestedDate;

        var resultingNext = scheduleChanged
            ? requestedDate
            : currentNext;

        var validation = ValidateDetails(
            description,
            amount,
            type,
            frequency,
            resultingStart,
            categoryId,
            accountId,
            endDate);

        if (validation.IsFailure)
            return validation;

        if (amount.Currency != Amount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        if (hasGeneratedTransactions && frequency != Frequency)
        {
            return Error.BusinessRule(
                "recurring.frequency_already_used",
                "Crie uma nova recorrência para alterar uma frequência que já gerou movimentos.");
        }

        if (hasGeneratedTransactions &&
            scheduleChanged &&
            requestedDate <= NormalizeDate(LastGenerateDate!.Value))
        {
            return Error.BusinessRule(
                "recurring.schedule_before_history",
                "A próxima ocorrência deve ser posterior à última ocorrência já gerada.");
        }

        var normalizedEndDate = endDate.HasValue
            ? NormalizeDate(endDate.Value)
            : (DateTime?)null;

        if (isActive &&
            normalizedEndDate.HasValue &&
            resultingNext > normalizedEndDate.Value)
        {
            return Error.BusinessRule(
                "recurring.end_before_next_occurrence",
                "A data de fim não pode ser anterior à próxima ocorrência.");
        }

        // Todas as validações acontecem antes de modificar a entidade.
        Description = description.Trim();
        Amount = amount;
        Type = type;
        Frequency = frequency;
        CategoryId = categoryId;
        AccountId = accountId;
        EndDate = normalizedEndDate;
        IsActive = isActive;

        if (scheduleChanged)
        {
            if (!hasGeneratedTransactions)
            {
                StartDate = requestedDate;
            }

            ScheduleAnchorDate = requestedDate;
            NextDueDate = requestedDate;
        }

        Touch();

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
            return Result.Success();

        if (EndDate.HasValue &&
            NormalizeDate(NextDueDate) > NormalizeDate(EndDate.Value))
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

        if (NormalizeDate(NextDueDate) > today)
        {
            return Error.BusinessRule(
                "recurring.not_due",
                "A próxima ocorrência ainda não venceu.");
        }

        if (EndDate.HasValue &&
            NormalizeDate(NextDueDate) > NormalizeDate(EndDate.Value))
        {
            return Error.BusinessRule(
                "recurring.finished",
                "A recorrência terminou.");
        }

        if (LastGenerateDate.HasValue &&
            NormalizeDate(NextDueDate) <= NormalizeDate(LastGenerateDate.Value))
        {
            return Error.BusinessRule(
                "recurring.invalid_schedule",
                "A agenda não avançou para uma nova ocorrência.");
        }

        return Result.Success();
    }

    private DateTime CalculateNextDueDate()
    {
        var nextDueDate = NormalizeDate(NextDueDate);

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

        var anchor = NormalizeDate(
            ScheduleAnchorDate ?? StartDate);

        var elapsedMonths =
            (nextDueDate.Year - anchor.Year) * 12
            + nextDueDate.Month
            - anchor.Month;

        return StartDate.AddMonths(elapsedMonths + monthsToAdd);
    }

    private static Result ValidateDetails(
        string description,
        Money? amount,
        TransactionType type,
        Frequency frequency,
        DateTime startDate,
        Guid categoryId,
        Guid? accountId,
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

        if (accountId.HasValue && accountId.Value == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da conta é inválido.");
        }

        return Result.Success();
    }

    private static Result ValidateBeneficiary(
        Guid? beneficiaryUserId,
        TransactionType type,
        Guid? familyId)
    {
        if (!beneficiaryUserId.HasValue)
            return Result.Success();

        if (beneficiaryUserId.Value == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        if (type != TransactionType.Income)
        {
            return Error.Validation(
                "A associação de beneficiários está disponível apenas para receitas.");
        }

        if (!familyId.HasValue)
        {
            return Error.Validation(
                "É necessário associar a receita a uma família.");
        }

        return Result.Success();
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        var localDate = date.Kind == DateTimeKind.Utc
            ? date.ToLocalTime()
            : date;

        return DateTime.SpecifyKind(
            localDate.Date,
            DateTimeKind.Unspecified);
    }

    
    public Result AssociateWithFamily(
        Guid familyId,
        Guid beneficiaryUserId,
        Guid financialPlanId)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "recurring.inactive",
                "Não é possível alterar o âmbito de uma recorrência inativa.");
        }

        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (beneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        if (financialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do plano financeiro é inválido.");
        }

        if (Scope == RecurringTransactionScope.Family)
        {
            if (FamilyId == familyId)
            {
                return Error.Conflict(
                    "recurring.already_family",
                    "A recorrência já pertence a esta família.");
            }

            return Error.BusinessRule(
                "recurring.family_change_not_allowed",
                "Não é possível transferir uma recorrência entre famílias.");
        }

        if (Scope != RecurringTransactionScope.Personal ||
            FamilyId.HasValue ||
            FinancialPlanId.HasValue ||
            BeneficiaryUserId.HasValue)
        {
            return Error.BusinessRule(
                "recurring.invalid_scope",
                "A recorrência possui associações familiares inconsistentes.");
        }

        if (Type != TransactionType.Income)
        {
            return Error.Validation(
                "Esta operação destina-se apenas a receitas recorrentes.");
        }

        var beneficiaryValidation = ValidateBeneficiary(
            beneficiaryUserId,
            Type,
            familyId);

        if (beneficiaryValidation.IsFailure)
        {
            return beneficiaryValidation;
        }

        // A alteração aplica-se à regra recorrente.
        // Não modifica transações anteriormente geradas.
        // A conta bancária mantém a sua titularidade.

        FamilyId = familyId;
        FinancialPlanId = financialPlanId;
        BeneficiaryUserId = beneficiaryUserId;
        Scope = RecurringTransactionScope.Family;

        Touch();

        return Result.Success();
    }

}