using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.UpdateRecurringTransaction;

public sealed class UpdateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecurringTransactionHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        UpdateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Obter a recorrência existente.

        var recurringTransaction =
            await _unitOfWork.RecurringTransactions
                .GetByIdAsync(command.RecurringTransactionId);

        if (recurringTransaction is null)
        {
            return Error.NotFound(
                "Recurring.NotFound",
                "Recorrência não encontrada.");
        }

        if (!recurringTransaction.IsActive)
        {
            return Error.Conflict(
                "Recurring.Inactive",
                "Não é possível editar uma recorrência inativa.");
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            return Error.Validation(
                "A descrição é obrigatória.");
        }

        if (command.AccountId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da conta é inválido.");
        }

        if (command.BeneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        var description = command.Description.Trim();

        // 2. Validar o responsável pela recorrência.

        var user = await _unitOfWork.Users
            .GetByIdAsync(recurringTransaction.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "O responsável pela recorrência não existe ou está inativo.");
        }

        // 3. Validar a família, quando aplicável.

        if (recurringTransaction.FamilyId.HasValue)
        {
            var family = await _unitOfWork.Families
                .GetByIdAsync(recurringTransaction.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "A família não existe ou está inativa.");
            }

            if (user.FamilyId != recurringTransaction.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.FamilyAccessDenied",
                    "O responsável não pertence à família da recorrência.");
            }
        }

        // 4. Determinar e validar o beneficiário.
        // Null preserva o beneficiário existente nas receitas.
        // Uma despesa nunca pode manter um beneficiário.

        Guid? beneficiaryUserId =
            command.Type == TransactionType.Income
                ? command.BeneficiaryUserId
                    ?? recurringTransaction.BeneficiaryUserId
                : null;

        if (command.Type != TransactionType.Income &&
            command.BeneficiaryUserId.HasValue)
        {
            return Error.Validation(
                "Só é possível associar beneficiários a receitas.");
        }

        if (beneficiaryUserId.HasValue)
        {
            if (!recurringTransaction.FamilyId.HasValue)
            {
                return Error.Validation(
                    "O beneficiário exige uma recorrência familiar.");
            }

            var beneficiary = await _unitOfWork.Users
                .GetByIdAsync(beneficiaryUserId.Value);

            if (beneficiary is null || !beneficiary.IsActive)
            {
                return Error.NotFound(
                    "Recurring.BeneficiaryNotFound",
                    "O beneficiário não existe ou está inativo.");
            }

            if (beneficiary.FamilyId !=
                recurringTransaction.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.BeneficiaryFamilyMismatch",
                    "O beneficiário não pertence à família da recorrência.");
            }
        }

        if (command.Type == TransactionType.Income &&
            recurringTransaction.FamilyId.HasValue &&
            !beneficiaryUserId.HasValue)
        {
            return Error.Validation(
                "Seleciona o membro da família que recebe esta receita.");
        }

        // 5. Validar a conta bancária opcional.

        if (!command.AccountId.HasValue &&
            !recurringTransaction.FinancialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência sem conta deve estar associada a um plano financeiro.");
        }

        if (command.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts
                .GetByIdAsync(command.AccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.Validation(
                    "A conta selecionada é inválida.");
            }

            if (recurringTransaction.FamilyId.HasValue)
            {
                if (account.FamilyId !=
                    recurringTransaction.FamilyId)
                {
                    return Error.BusinessRule(
                        "Recurring.AccountFamilyMismatch",
                        "A conta não pertence à família da recorrência.");
                }
            }
            else if (account.UserId != recurringTransaction.UserId ||
                     account.FamilyId.HasValue)
            {
                return Error.BusinessRule(
                    "Recurring.AccountOwnerMismatch",
                    "A conta não pertence ao utilizador.");
            }

            if (!string.Equals(
                    account.Balance.Currency,
                    recurringTransaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // 6. Validar o plano financeiro existente.

        if (recurringTransaction.FinancialPlanId.HasValue)
        {
            var financialPlan = await _unitOfWork.FinancialPlans
                .GetByIdAsync(
                    recurringTransaction.FinancialPlanId.Value);

            if (financialPlan is null || !financialPlan.IsActive)
            {
                return Error.NotFound(
                    "FinancialPlan.NotFound",
                    "O plano financeiro não existe ou está inativo.");
            }

            if (!recurringTransaction.FamilyId.HasValue ||
                financialPlan.FamilyId !=
                    recurringTransaction.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.FinancialPlanFamilyMismatch",
                    "O plano financeiro não pertence à família.");
            }

            if (!string.Equals(
                    financialPlan.CurrencyCode,
                    recurringTransaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // 7. Validar a categoria e o seu âmbito.

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.Validation(
                "A categoria selecionada é inválida.");
        }

        if (!category.IsGlobal)
        {
            if (recurringTransaction.FamilyId.HasValue)
            {
                if (category.FamilyId !=
                    recurringTransaction.FamilyId)
                {
                    return Error.BusinessRule(
                        "Recurring.CategoryScopeMismatch",
                        "A categoria não pertence à família.");
                }
            }
            else if (category.FamilyId.HasValue ||
                     category.UserId != recurringTransaction.UserId)
            {
                return Error.BusinessRule(
                    "Recurring.CategoryScopeMismatch",
                    "A categoria não pertence ao utilizador.");
            }
        }

        // 8. Verificar duplicados.
        // Nas receitas familiares, a combinação relevante
        // é família + beneficiário + descrição.

        if (command.Type == TransactionType.Income &&
            recurringTransaction.FamilyId.HasValue &&
            beneficiaryUserId.HasValue)
        {
            var familyId = recurringTransaction.FamilyId.Value;

            var familyIncome = await _unitOfWork
                .RecurringTransactions
                .FindRecurringTransactionsWithDetailsAsync(
                    rt => rt.IsActive &&
                          rt.FamilyId == familyId &&
                          rt.Type == TransactionType.Income);

            var duplicate = familyIncome.Any(rt =>
                rt.Id != recurringTransaction.Id &&
                rt.BeneficiaryUserId == beneficiaryUserId &&
                string.Equals(
                    rt.Description.Trim(),
                    description,
                    StringComparison.OrdinalIgnoreCase));

            if (duplicate)
            {
                return Error.Conflict(
                    "Recurring.Duplicate",
                    "Este beneficiário já tem uma receita recorrente com esta descrição.");
            }
        }
        else if (!recurringTransaction.Description.Equals(
                     description,
                     StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _unitOfWork
                .RecurringTransactions
                .RecurringTransactionExistsForUserByDescriptionAndIdAsync(
                    description,
                    recurringTransaction.UserId,
                    recurringTransaction.Id);

            if (exists)
            {
                return Error.Conflict(
                    "Recurring.Duplicate",
                    "Já existe outra recorrência com esta descrição.");
            }
        }

        // 9. Validar o valor.

        var moneyResult = Money.TryCreate(
            command.Amount,
            recurringTransaction.Amount.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 10. Atualizar os detalhes.
        // O Domain mantém a proteção contra alterações
        // de frequência após a geração de movimentos.

        var updateResult = recurringTransaction.UpdateDetails(
            description: description,
            amount: moneyResult.Value,
            type: command.Type,
            frequency: command.Frequency,
            categoryId: command.CategoryId,
            accountId: command.AccountId,
            endDate: command.EndDate,
            isActive: recurringTransaction.IsActive);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        // 11. Atualizar o beneficiário.

        var beneficiaryResult =
            recurringTransaction.AssignBeneficiary(
                beneficiaryUserId);

        if (beneficiaryResult.IsFailure)
        {
            return beneficiaryResult.Error;
        }

        // 12. Persistir a recorrência existente.

        _unitOfWork.RecurringTransactions.Update(
            recurringTransaction);

        cancellationToken.ThrowIfCancellationRequested();

        await _unitOfWork.CompleteAsync();

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}