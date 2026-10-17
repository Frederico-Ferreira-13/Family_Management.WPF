
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;

public sealed class CreateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecurringTransactionHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        CreateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        if (command.CategoryId == Guid.Empty)
            return Error.Validation("A categoria é obrigatória.");

        if (command.AccountId == Guid.Empty)
            return Error.Validation("O identificador da conta é inválido.");

        if (command.FamilyId == Guid.Empty)
            return Error.Validation("O identificador da família é inválido.");

        if (command.FinancialPlanId == Guid.Empty)
            return Error.Validation("O identificador do plano financeiro é inválido.");

        if (!command.AccountId.HasValue &&
            !command.FinancialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência sem conta bancária deve estar associada a um plano financeiro.");
        }

        // Utilizador
        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        // Família
        if (command.FamilyId.HasValue)
        {
            if (user.FamilyId != command.FamilyId)
            {
                return Error.BusinessRule(
                    "Recurring.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(command.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "Família não encontrada ou inativa.");
            }
        }

        // Plano financeiro
        FinancialPlan? financialPlan = null;

        if (command.FinancialPlanId.HasValue)
        {
            financialPlan = await _unitOfWork.FinancialPlans
                .GetByIdAsync(command.FinancialPlanId.Value);

            if (financialPlan is null || !financialPlan.IsActive)
            {
                return Error.NotFound(
                    "FinancialPlan.NotFound",
                    "Plano financeiro não encontrado ou inativo.");
            }

            if (!command.FamilyId.HasValue ||
                financialPlan.FamilyId != command.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.FinancialPlanFamilyMismatch",
                    "O plano financeiro não pertence à família indicada.");
            }
        }

        // Conta bancária opcional
        Account? account = null;

        if (command.AccountId.HasValue)
        {
            account = await _unitOfWork.Accounts
                .GetByIdAsync(command.AccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "Conta bancária não encontrada ou inativa.");
            }

            if (command.FamilyId.HasValue)
            {
                if (account.FamilyId != command.FamilyId)
                {
                    return Error.BusinessRule(
                        "Recurring.AccountFamilyMismatch",
                        "A conta não pertence à família indicada.");
                }
            }
            else if (account.UserId != command.UserId ||
                     account.FamilyId.HasValue)
            {
                return Error.BusinessRule(
                    "Recurring.AccountOwnerMismatch",
                    "A conta não pertence ao utilizador.");
            }
        }

        // Categoria
        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.NotFound(
                "Category.NotFound",
                "A categoria selecionada é inválida.");
        }

        if (!category.IsGlobal)
        {
            if (command.FamilyId.HasValue)
            {
                if (category.FamilyId != command.FamilyId)
                {
                    return Error.BusinessRule(
                        "Recurring.CategoryScopeMismatch",
                        "A categoria não pertence à família indicada.");
                }
            }
            else if (category.FamilyId.HasValue ||
                     category.UserId != command.UserId)
            {
                return Error.BusinessRule(
                    "Recurring.CategoryScopeMismatch",
                    "A categoria não pertence ao utilizador.");
            }
        }

        // Evitar recorrências duplicadas
        var exists = await _unitOfWork.RecurringTransactions
            .RecurringTransactionExistsForUserByDescriptionAsync(
                command.Description,
                command.UserId);

        if (exists)
        {
            return Error.Conflict(
                "Recurring.Duplicate",
                "Já existe uma recorrência ativa com esta descrição.");
        }

        // Valor monetário
        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
            return moneyResult.Error;

        var money = moneyResult.Value;

        if (account is not null &&
            !string.Equals(
                account.Balance.Currency,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        if (financialPlan is not null &&
            !string.Equals(
                financialPlan.CurrencyCode,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        // Criar recorrência
        var recurringResult = RecurringTransaction.Create(
            description: command.Description,
            amount: money,
            type: command.Type,
            frequency: command.Frequency,
            startDate: command.StartDate,
            categoryId: command.CategoryId,
            accountId: command.AccountId,
            userId: command.UserId,
            familyId: command.FamilyId,
            financialPlanId: command.FinancialPlanId,
            endDate: command.EndDate);

        if (recurringResult.IsFailure)
            return recurringResult.Error;

        var recurringTransaction = recurringResult.Value;

        await _unitOfWork.RecurringTransactions
            .AddAsync(recurringTransaction);

        await _unitOfWork.CompleteAsync();

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}