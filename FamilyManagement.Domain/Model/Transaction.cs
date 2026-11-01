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

    public Guid? FamilyId { get; private set; }

    public RecurringTransactionScope Scope { get; private set; }

    public bool IsFamilyTransaction =>
        Scope == RecurringTransactionScope.Family;

    public bool IsPersonalTransaction =>
        Scope == RecurringTransactionScope.Personal;

    public Guid? BeneficiaryUserId { get; private set; }

    public Guid? AccountId { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Guid? TargetAccountId { get; private set; }

    public Guid? RecurringTransactionId { get; private set; }

    public Guid? BudgetId { get; private set; }

    public Guid? GoalId { get; private set; }

    public Guid? FinancialPlanId { get; private set; }

    public DateTime? ScheduledDate { get; private set; }

    public virtual User? User { get; private set; }

    public virtual Family? Family { get; private set; }

    public virtual User? BeneficiaryUser { get; private set; }

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

    public virtual FinancialPlan? FinancialPlan { get; private set; }

    private Transaction()
    {
    }

    private Transaction(
        DateTime date,
        Money amount,
        string description,
        TransactionType type,
        Guid userId,
        Guid? familyId,
        Guid? accountId,
        Guid? categoryId,
        Guid? targetAccountId,
        string? notes,
        Guid? budgetId,
        Guid? goalId,
        Guid? recurringTransactionId,
        bool isConfirmed,
        Guid? financialPlanId,
        Guid? beneficiaryUserId)
    {
        Date = NormalizeDate(date);

        ScheduledDate = recurringTransactionId.HasValue
            ? Date
            : null;

        Amount = amount;
        Description = description.Trim();
        Notes = NormalizeNotes(notes);

        Type = type;

        UserId = userId;
        FamilyId = familyId;

        Scope = familyId.HasValue
            ? RecurringTransactionScope.Family
            : RecurringTransactionScope.Personal;

        BeneficiaryUserId = beneficiaryUserId;

        AccountId = accountId;
        CategoryId = categoryId;
        TargetAccountId = targetAccountId;

        BudgetId = budgetId;
        GoalId = goalId;

        RecurringTransactionId = recurringTransactionId;

        IsConfirmed = isConfirmed;

        IsRecurringGenerated =
            recurringTransactionId.HasValue;

        FinancialPlanId = financialPlanId;
    }

    // =========================================================
    // CRIAR RECEITA OU DESPESA
    // =========================================================

    public static Result<Transaction> CreateIncomeOrExpense(
        DateTime date,
        Money amount,
        string description,
        TransactionType type,
        Guid userId,
        Guid? accountId,
        Guid categoryId,
        string? notes = null,
        Guid? budgetId = null,
        Guid? goalId = null,
        bool isConfirmed = true,
        bool isRecurringGenerated = false,
        DateTime? referenceDate = null,
        Guid? familyId = null,
        Guid? financialPlanId = null,
        Guid? beneficiaryUserId = null)
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
        {
            return Error.Validation(
                "A categoria é obrigatória.");
        }

        if (budgetId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do orçamento é inválido.");
        }

        if (goalId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da meta é inválido.");
        }

        if (isRecurringGenerated)
        {
            return Error.Validation(
                "Utilize CreateFromRecurring para criar um movimento de uma recorrência.");
        }

        var classificationValidation =
            ValidateClassification(
                type,
                familyId,
                financialPlanId,
                beneficiaryUserId);

        if (classificationValidation.IsFailure)
            return classificationValidation.Error;

        if (!accountId.HasValue &&
            !financialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma receita ou despesa sem conta bancária deve estar associada a um plano financeiro.");
        }

        return new Transaction(
            date: date,
            amount: amount,
            description: description,
            type: type,
            userId: userId,
            familyId: familyId,
            accountId: accountId,
            categoryId: categoryId,
            targetAccountId: null,
            notes: notes,
            budgetId: budgetId,
            goalId: goalId,
            recurringTransactionId: null,
            isConfirmed: isConfirmed,
            financialPlanId: financialPlanId,
            beneficiaryUserId: beneficiaryUserId);
    }

    // =========================================================
    // CRIAR TRANSFERÊNCIA
    // =========================================================

    public static Result<Transaction> CreateTransfer(
        DateTime date,
        Money amount,
        string description,
        Guid userId,
        Guid sourceAccountId,
        Guid targetAccountId,
        string? notes = null,
        bool isConfirmed = true,
        DateTime? referenceDate = null,
        Guid? familyId = null,
        Guid? financialPlanId = null)
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

        var classificationValidation =
            ValidateClassification(
                TransactionType.Transfer,
                familyId,
                financialPlanId,
                beneficiaryUserId: null);

        if (classificationValidation.IsFailure)
            return classificationValidation.Error;

        return new Transaction(
            date: date,
            amount: amount,
            description: description,
            type: TransactionType.Transfer,
            userId: userId,
            familyId: familyId,
            accountId: sourceAccountId,
            categoryId: null,
            targetAccountId: targetAccountId,
            notes: notes,
            budgetId: null,
            goalId: null,
            recurringTransactionId: null,
            isConfirmed: isConfirmed,
            financialPlanId: financialPlanId,
            beneficiaryUserId: null);
    }

    // =========================================================
    // CRIAR MOVIMENTO A PARTIR DE RECORRÊNCIA
    // =========================================================

    public static Result<Transaction> CreateFromRecurring(
        DateTime date,
        Money amount,
        string description,
        TransactionType type,
        Guid userId,
        Guid? accountId,
        Guid categoryId,
        Guid recurringTransactionId,
        bool isConfirmed = false,
        DateTime? referenceDate = null,
        Guid? familyId = null,
        Guid? financialPlanId = null,
        Guid? beneficiaryUserId = null)
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
        {
            return Error.Validation(
                "A categoria é obrigatória.");
        }

        if (recurringTransactionId == Guid.Empty)
        {
            return Error.Validation(
                "A identificação da recorrência é obrigatória.");
        }

        var classificationValidation =
            ValidateClassification(
                type,
                familyId,
                financialPlanId,
                beneficiaryUserId);

        if (classificationValidation.IsFailure)
            return classificationValidation.Error;

        if (!accountId.HasValue &&
            !financialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência sem conta bancária deve estar associada a um plano financeiro.");
        }

        return new Transaction(
            date: date,
            amount: amount,
            description: description,
            type: type,
            userId: userId,
            familyId: familyId,
            accountId: accountId,
            categoryId: categoryId,
            targetAccountId: null,
            notes: null,
            budgetId: null,
            goalId: null,
            recurringTransactionId: recurringTransactionId,
            isConfirmed: isConfirmed,
            financialPlanId: financialPlanId,
            beneficiaryUserId: beneficiaryUserId);
    }

    // =========================================================
    // RECLASSIFICAR COMO MOVIMENTO FAMILIAR
    // =========================================================

    public Result ReclassifyAsFamily(
        Guid familyId,
        Guid? financialPlanId = null,
        Guid? beneficiaryUserId = null)
    {
        if (!IsActive)
            return InactiveError();

        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "A família é obrigatória.");
        }

        if (financialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do plano financeiro é inválido.");
        }

        if (beneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        if (beneficiaryUserId.HasValue &&
            Type != TransactionType.Income)
        {
            return Error.Validation(
                "Apenas receitas podem ter um beneficiário.");
        }

        if (Scope == RecurringTransactionScope.Family &&
            FamilyId == familyId &&
            FinancialPlanId == financialPlanId &&
            BeneficiaryUserId == beneficiaryUserId)
        {
            return Result.Success();
        }

        // A validação de pertença do utilizador à família,
        // beneficiário, conta e plano financeiro pertence
        // ao handler de Application, que tem acesso aos
        // respetivos repositórios.

        FamilyId = familyId;
        Scope = RecurringTransactionScope.Family;

        FinancialPlanId = financialPlanId;
        BeneficiaryUserId = beneficiaryUserId;

        // A reclassificação não altera:
        // - AccountId
        // - Amount
        // - Date
        // - IsConfirmed
        // - RecurringTransactionId

        Touch();

        return Result.Success();
    }

    // =========================================================
    // RECLASSIFICAR COMO MOVIMENTO PESSOAL
    // =========================================================

    public Result ReclassifyAsPersonal()
    {
        if (!IsActive)
            return InactiveError();

        if (Scope == RecurringTransactionScope.Personal &&
            !FamilyId.HasValue &&
            !FinancialPlanId.HasValue &&
            !BeneficiaryUserId.HasValue)
        {
            return Result.Success();
        }

        // Relações com orçamento e objetivo devem ser
        // verificadas pelo handler antes da operação.

        FamilyId = null;
        Scope = RecurringTransactionScope.Personal;

        FinancialPlanId = null;
        BeneficiaryUserId = null;

        // Mantemos a conta, o valor e a confirmação.

        Touch();

        return Result.Success();
    }

    // =========================================================
    // ALTERAR BENEFICIÁRIO
    // =========================================================

    public Result AssignBeneficiary(Guid? beneficiaryUserId)
    {
        if (!IsActive)
            return InactiveError();

        if (beneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        if (beneficiaryUserId.HasValue)
        {
            if (Scope != RecurringTransactionScope.Family)
            {
                return Error.Validation(
                    "O beneficiário exige um movimento familiar.");
            }

            if (Type != TransactionType.Income)
            {
                return Error.Validation(
                    "Apenas receitas podem ter um beneficiário.");
            }
        }

        if (BeneficiaryUserId == beneficiaryUserId)
            return Result.Success();

        BeneficiaryUserId = beneficiaryUserId;

        Touch();

        return Result.Success();
    }

    // =========================================================
    // CONFIRMAR MOVIMENTO
    // =========================================================

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

    // =========================================================
    // RETIRAR CONFIRMAÇÃO
    // =========================================================

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

    // =========================================================
    // ATUALIZAR NOTAS
    // =========================================================

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

    // =========================================================
    // ATUALIZAR DETALHES
    // =========================================================

    public Result UpdateDetails(
        string? description,
        string? notes,
        Guid? categoryId,
        Money? amount,
        Guid? accountId,
        bool? isConfirmed,
        DateTime? referenceDate = null,
        DateTime? date = null)
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

        var resultingDate = NormalizeDate(date ?? Date);

        if (resultingDate == default)
        {
            return Error.Validation(
                "A data do movimento é obrigatória.");
        }

        var resultingConfirmation = isConfirmed ?? IsConfirmed;

        var today = NormalizeDate(
            referenceDate ?? DateTime.Today);

        if (resultingConfirmation && resultingDate > today)
        {
            return Error.BusinessRule(
                "transaction.future_confirmation",
                "Não é possível confirmar um movimento com data futura.");
        }

        // Preserva a ocorrência original, incluindo registos antigos.
        if (RecurringTransactionId.HasValue &&
            !ScheduledDate.HasValue)
        {
            ScheduledDate = NormalizeDate(Date);
        }

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

        Date = resultingDate;
        Notes = NormalizeNotes(notes);
        IsConfirmed = resultingConfirmation;

        Touch();

        return Result.Success();
    }

    // =========================================================
    // VALIDAÇÃO DA CLASSIFICAÇÃO FINANCEIRA
    // =========================================================

    private static Result ValidateClassification(
        TransactionType type,
        Guid? familyId,
        Guid? financialPlanId,
        Guid? beneficiaryUserId)
    {
        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (financialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do plano financeiro é inválido.");
        }

        if (beneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        if (!familyId.HasValue)
        {
            if (financialPlanId.HasValue ||
                beneficiaryUserId.HasValue)
            {
                return Error.Validation(
                    "Um movimento pessoal não pode ter um plano financeiro ou beneficiário familiar.");
            }
        }

        if (beneficiaryUserId.HasValue &&
            type != TransactionType.Income)
        {
            return Error.Validation(
                "Apenas receitas podem ter um beneficiário.");
        }

        return Result.Success();
    }

    // =========================================================
    // VALIDAÇÕES BASE
    // =========================================================

    private static Result ValidateBase(
        DateTime date,
        Money? amount,
        string description,
        Guid userId,
        Guid? accountId,
        bool isConfirmed,
        DateTime? referenceDate)
    {
        if (date == default)
            return Error.Validation(
                "A data é obrigatória.");

        if (amount is null || amount.Amount <= 0)
            return DomainErrors.Transaction.InvalidAmount;

        if (string.IsNullOrWhiteSpace(description))
            return Error.Validation(
                "A descrição é obrigatória.");

        if (userId == Guid.Empty)
            return Error.Validation(
                "O utilizador é obrigatório.");

        if (accountId == Guid.Empty)
            return Error.Validation(
                "A conta é obrigatória.");

        var transactionDate =
            NormalizeDate(date);

        var today =
            NormalizeDate(referenceDate ?? DateTime.Today);

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
