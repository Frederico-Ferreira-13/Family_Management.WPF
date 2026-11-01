
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions
    .Commands.AssociateRecurringTransactionWithFamily;

public sealed class AssociateRecurringTransactionWithFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AssociateRecurringTransactionWithFamilyHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        AssociateRecurringTransactionWithFamilyCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validar sessão e identificadores.

        if (!_currentUserService.IsAuthenticated ||
            !_currentUserService.UserId.HasValue)
        {
            return Error.BusinessRule(
                "Recurring.AuthenticationRequired",
                "É necessário iniciar sessão.");
        }

        var currentUserId = _currentUserService.UserId.Value;

        if (command.RequestedByUserId == Guid.Empty ||
            command.RequestedByUserId != currentUserId)
        {
            return Error.BusinessRule(
                "Recurring.AccessDenied",
                "O utilizador indicado não corresponde à sessão atual.");
        }

        if (command.RecurringTransactionId == Guid.Empty ||
            command.FamilyId == Guid.Empty ||
            command.BeneficiaryUserId == Guid.Empty ||
            command.FinancialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "Os identificadores da operação são inválidos.");
        }

        // 2. Obter a recorrência.

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
                "Não é possível converter uma recorrência inativa.");
        }

        if (recurringTransaction.UserId != currentUserId)
        {
            return Error.BusinessRule(
                "Recurring.AccessDenied",
                "Apenas o responsável pode converter esta recorrência.");
        }

        if (recurringTransaction.Scope ==
            RecurringTransactionScope.Family)
        {
            return Error.Conflict(
                "Recurring.AlreadyFamily",
                "A recorrência já está associada a uma família.");
        }

        if (recurringTransaction.Scope !=
                RecurringTransactionScope.Personal ||
            recurringTransaction.FamilyId.HasValue ||
            recurringTransaction.FinancialPlanId.HasValue ||
            recurringTransaction.BeneficiaryUserId.HasValue)
        {
            return Error.BusinessRule(
                "Recurring.InvalidScope",
                "A recorrência possui associações familiares inconsistentes.");
        }

        if (recurringTransaction.Type != TransactionType.Income)
        {
            return Error.Validation(
                "Esta operação destina-se apenas a receitas recorrentes.");
        }

        // 3. Validar o responsável.

        var user = await _unitOfWork.Users
            .GetByIdAsync(currentUserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "O responsável não existe ou está inativo.");
        }

        if (user.FamilyId != command.FamilyId)
        {
            return Error.BusinessRule(
                "Recurring.FamilyAccessDenied",
                "O responsável não pertence à família indicada.");
        }

        // 4. Validar a família.

        var family = await _unitOfWork.Families
            .GetByIdAsync(command.FamilyId);

        if (family is null || !family.IsActive)
        {
            return Error.NotFound(
                "Family.NotFound",
                "A família não existe ou está inativa.");
        }

        // 5. Validar o beneficiário.

        var beneficiary = await _unitOfWork.Users
            .GetByIdAsync(command.BeneficiaryUserId);

        if (beneficiary is null || !beneficiary.IsActive)
        {
            return Error.NotFound(
                "Recurring.BeneficiaryNotFound",
                "O beneficiário não existe ou está inativo.");
        }

        if (beneficiary.FamilyId != command.FamilyId)
        {
            return Error.BusinessRule(
                "Recurring.BeneficiaryFamilyMismatch",
                "O beneficiário não pertence à família indicada.");
        }

        // 6. Validar o plano financeiro.

        var financialPlan = await _unitOfWork.FinancialPlans
            .GetByIdAsync(command.FinancialPlanId);

        if (financialPlan is null || !financialPlan.IsActive)
        {
            return Error.NotFound(
                "FinancialPlan.NotFound",
                "O plano financeiro não existe ou está inativo.");
        }

        if (financialPlan.FamilyId != command.FamilyId)
        {
            return Error.BusinessRule(
                "Recurring.FinancialPlanFamilyMismatch",
                "O plano financeiro não pertence à família indicada.");
        }

        if (!string.Equals(
                financialPlan.CurrencyCode,
                recurringTransaction.Amount.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        // 7. Validar a conta bancária.
        // Pode ser pessoal do responsável ou familiar.
        // A titularidade não será modificada.

        if (recurringTransaction.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts
                .GetByIdAsync(recurringTransaction.AccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "A conta associada não existe ou está inativa.");
            }

            var isFamilyAccount =
                account.FamilyId == command.FamilyId;

            var isOwnPersonalAccount =
                !account.FamilyId.HasValue &&
                account.UserId == currentUserId;

            if (!isFamilyAccount && !isOwnPersonalAccount)
            {
                return Error.BusinessRule(
                    "Recurring.AccountScopeMismatch",
                    "A conta não pertence à família nem ao responsável.");
            }

            if (!string.Equals(
                    account.Balance.Currency,
                    recurringTransaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // 8. Validar a categoria.

        var category = await _unitOfWork.Categories
            .GetByIdAsync(recurringTransaction.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.NotFound(
                "Category.NotFound",
                "A categoria associada não existe ou está inativa.");
        }

        if (category.Type != CategoryType.Income)
        {
            return Error.Validation(
                "A categoria deve ser do tipo receita.");
        }

        if (!category.IsGlobal)
        {
            var isFamilyCategory =
                category.FamilyId == command.FamilyId;

            var isOwnPersonalCategory =
                !category.FamilyId.HasValue &&
                category.UserId == currentUserId;

            if (!isFamilyCategory && !isOwnPersonalCategory)
            {
                return Error.BusinessRule(
                    "Recurring.CategoryScopeMismatch",
                    "A categoria não pertence à família nem ao responsável.");
            }
        }

        // 9. Verificar duplicados familiares.

        var familyIncome = await _unitOfWork
            .RecurringTransactions
            .FindRecurringTransactionsWithDetailsAsync(
                item =>
                    item.IsActive &&
                    item.FamilyId == command.FamilyId &&
                    item.Type == TransactionType.Income);

        var duplicate = familyIncome.Any(item =>
            item.Id != recurringTransaction.Id &&
            item.BeneficiaryUserId == command.BeneficiaryUserId &&
            string.Equals(
                item.Description.Trim(),
                recurringTransaction.Description.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Error.Conflict(
                "Recurring.Duplicate",
                "O beneficiário já tem uma receita recorrente com esta descrição.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 10. Alterar a classificação no Domain.

        var conversionResult =
            recurringTransaction.AssociateWithFamily(
                command.FamilyId,
                command.BeneficiaryUserId,
                command.FinancialPlanId);

        if (conversionResult.IsFailure)
        {
            return conversionResult.Error;
        }

        // 11. Persistir.
        // Não alteramos transações já geradas
        // nem aplicamos impactos aos saldos bancários.

        cancellationToken.ThrowIfCancellationRequested();

        _unitOfWork.RecurringTransactions.Update(
            recurringTransaction);

        await _unitOfWork.CompleteAsync();

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}
