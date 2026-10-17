
using System;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.UpdateTransaction;

public sealed class UpdateTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public UpdateTransactionHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _impactService = impactService
            ?? throw new ArgumentNullException(nameof(impactService));
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        UpdateTransactionCommand command)
    {
        // --------------------------------------------------
        // 1. Validar identificadores
        // --------------------------------------------------

        if (command.TransactionId == Guid.Empty)
        {
            return Error.Validation(
                "A transação é obrigatória.");
        }

        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (command.AccountId == Guid.Empty)
        {
            return Error.Validation(
                "A conta indicada é inválida.");
        }

        if (command.CategoryId == Guid.Empty)
        {
            return Error.Validation(
                "A categoria indicada é inválida.");
        }

        // --------------------------------------------------
        // 2. Validar utilizador
        // --------------------------------------------------

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        // --------------------------------------------------
        // 3. Obter transação
        // --------------------------------------------------

        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(command.TransactionId);

        if (transaction is null || !transaction.IsActive)
        {
            return Error.NotFound(
                "Transaction.NotFound",
                "Transação não encontrada ou inativa.");
        }

        // --------------------------------------------------
        // 4. Validar autorização
        // --------------------------------------------------

        if (transaction.FamilyId.HasValue)
        {
            if (user.FamilyId != transaction.FamilyId)
            {
                return Error.BusinessRule(
                    "Transaction.AccessDenied",
                    "Não tem autorização para editar esta transação familiar.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(transaction.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.BusinessRule(
                    "Transaction.FamilyUnavailable",
                    "A família não existe ou está inativa.");
            }
        }
        else if (transaction.UserId != command.UserId)
        {
            return Error.BusinessRule(
                "Transaction.AccessDenied",
                "Não tem autorização para editar esta transação pessoal.");
        }

        // --------------------------------------------------
        // 5. Preparar novo valor monetário
        // --------------------------------------------------

        Money? newAmount = null;

        if (command.Amount.HasValue)
        {
            var moneyResult = Money.TryCreate(
                command.Amount.Value,
                transaction.Amount.Currency);

            if (moneyResult.IsFailure)
                return moneyResult.Error;

            newAmount = moneyResult.Value;
        }

        // --------------------------------------------------
        // 6. Determinar conta e categoria resultantes
        // --------------------------------------------------

        // Null no comando significa manter a conta atual.
        // A remoção explícita da conta deverá ser tratada
        // por uma operação específica.
        Guid? resultingAccountId =
            command.AccountId ?? transaction.AccountId;

        Guid? resultingCategoryId =
            command.CategoryId ?? transaction.CategoryId;

        // --------------------------------------------------
        // 7. Validar conta bancária opcional
        // --------------------------------------------------

        Account? account = null;

        if (resultingAccountId.HasValue)
        {
            account = await _unitOfWork.Accounts
                .GetByIdAsync(resultingAccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "Conta não encontrada ou inativa.");
            }

            var accountScopeResult =
                ValidateAccountScope(transaction, account);

            if (accountScopeResult.IsFailure)
                return accountScopeResult.Error;

            if (!string.Equals(
                    account.Balance.Currency,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }
        else
        {
            // Transferências exigem conta bancária.
            if (transaction.Type == TransactionType.Transfer)
            {
                return Error.Validation(
                    "Uma transferência exige uma conta de origem.");
            }

            // Receitas e despesas sem conta têm de estar
            // associadas a um plano financeiro.
            if (!transaction.FinancialPlanId.HasValue)
            {
                return Error.Validation(
                    "Um movimento sem conta bancária deve estar associado a um plano financeiro.");
            }
        }

        // --------------------------------------------------
        // 8. Validar transferência ou categoria
        // --------------------------------------------------

        if (transaction.Type == TransactionType.Transfer)
        {
            if (command.CategoryId.HasValue)
            {
                return Error.Validation(
                    "Uma transferência não utiliza categoria.");
            }

            if (!transaction.TargetAccountId.HasValue)
            {
                return Error.Validation(
                    "A transferência não possui conta de destino.");
            }

            if (resultingAccountId == transaction.TargetAccountId.Value)
            {
                return Error.Validation(
                    "As contas de origem e destino devem ser diferentes.");
            }

            var targetAccount = await _unitOfWork.Accounts
                .GetByIdAsync(transaction.TargetAccountId.Value);

            if (targetAccount is null || !targetAccount.IsActive)
            {
                return Error.NotFound(
                    "Account.TargetNotFound",
                    "Conta de destino não encontrada ou inativa.");
            }

            var targetScopeResult =
                ValidateAccountScope(transaction, targetAccount);

            if (targetScopeResult.IsFailure)
                return targetScopeResult.Error;

            if (!string.Equals(
                    targetAccount.Balance.Currency,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }
        else
        {
            if (!resultingCategoryId.HasValue)
            {
                return Error.Validation(
                    "A categoria é obrigatória.");
            }

            var category = await _unitOfWork.Categories
                .GetByIdAsync(resultingCategoryId.Value);

            if (category is null || !category.IsActive)
            {
                return Error.NotFound(
                    "Category.NotFound",
                    "Categoria não encontrada ou inativa.");
            }

            var categoryScopeResult =
                ValidateCategoryScope(transaction, category);

            if (categoryScopeResult.IsFailure)
                return categoryScopeResult.Error;

            var expectedType =
                transaction.Type == TransactionType.Income
                    ? CategoryType.Income
                    : CategoryType.Expense;

            if (category.Type != expectedType)
            {
                return Error.BusinessRule(
                    "Transaction.CategoryTypeMismatch",
                    "A categoria não corresponde ao tipo de transação.");
            }
        }

        // --------------------------------------------------
        // 9. Validar orçamento associado
        // --------------------------------------------------

        if (transaction.BudgetId.HasValue)
        {
            var budget = await _unitOfWork.Budgets
                .GetByIdAsync(transaction.BudgetId.Value);

            if (budget is null || !budget.IsActive)
            {
                return Error.NotFound(
                    "Budget.NotFound",
                    "Orçamento associado não encontrado ou inativo.");
            }

            if (budget.FamilyId != transaction.FamilyId ||
                (!transaction.FamilyId.HasValue &&
                 budget.UserId != transaction.UserId))
            {
                return Error.BusinessRule(
                    "Transaction.BudgetScopeMismatch",
                    "O orçamento não pertence ao âmbito da transação.");
            }

            if (budget.CategoryId != resultingCategoryId)
            {
                return Error.BusinessRule(
                    "Transaction.BudgetCategoryMismatch",
                    "A nova categoria não corresponde ao orçamento associado.");
            }

            if (budget.Month != transaction.Date.Month ||
                budget.Year != transaction.Date.Year)
            {
                return Error.BusinessRule(
                    "Transaction.BudgetPeriodMismatch",
                    "O orçamento não corresponde ao período da transação.");
            }

            if (!string.Equals(
                    budget.BudgetedAmount.Currency,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // --------------------------------------------------
        // 10. Validar meta financeira associada
        // --------------------------------------------------

        if (transaction.GoalId.HasValue)
        {
            var goal = await _unitOfWork.Goals
                .GetByIdAsync(transaction.GoalId.Value);

            if (goal is null || !goal.IsActive)
            {
                return Error.NotFound(
                    "Goal.NotFound",
                    "Meta associada não encontrada ou inativa.");
            }

            if (goal.FamilyId != transaction.FamilyId ||
                (!transaction.FamilyId.HasValue &&
                 goal.UserId != transaction.UserId))
            {
                return Error.BusinessRule(
                    "Transaction.GoalScopeMismatch",
                    "A meta não pertence ao âmbito da transação.");
            }

            if (!string.Equals(
                    goal.TargetAmount.Currency,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // --------------------------------------------------
        // 11. Validar plano financeiro associado
        // --------------------------------------------------

        if (transaction.FinancialPlanId.HasValue)
        {
            var financialPlan = await _unitOfWork.FinancialPlans
                .GetByIdAsync(transaction.FinancialPlanId.Value);

            if (financialPlan is null || !financialPlan.IsActive)
            {
                return Error.NotFound(
                    "FinancialPlan.NotFound",
                    "Plano financeiro não encontrado ou inativo.");
            }

            if (!transaction.FamilyId.HasValue ||
                financialPlan.FamilyId != transaction.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Transaction.FinancialPlanScopeMismatch",
                    "O plano financeiro não pertence à família da transação.");
            }

            if (!string.Equals(
                    financialPlan.CurrencyCode,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // --------------------------------------------------
        // 12. Reverter, atualizar e reaplicar impactos
        // --------------------------------------------------

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            // A confirmação da transação mantém-se.
            // Apenas movimentos confirmados afetam
            // saldos, orçamentos e metas.
            if (transaction.IsConfirmed)
            {
                var revertResult =
                    await _impactService.RevertAsync(transaction);

                if (revertResult.IsFailure)
                {
                    await RollbackAndClearAsync(dbTransaction);
                    return revertResult.Error;
                }
            }

            var updateResult = transaction.UpdateDetails(
                command.Description,
                command.Notes,
                command.CategoryId,
                newAmount,
                command.AccountId,
                isConfirmed: null);

            if (updateResult.IsFailure)
            {
                await RollbackAndClearAsync(dbTransaction);
                return updateResult.Error;
            }

            if (transaction.IsConfirmed)
            {
                var applyResult =
                    await _impactService.ApplyAsync(transaction);

                if (applyResult.IsFailure)
                {
                    await RollbackAndClearAsync(dbTransaction);
                    return applyResult.Error;
                }
            }

            _unitOfWork.Transactions.Update(transaction);

            await _unitOfWork.CompleteAsync();

            await dbTransaction.CommitAsync();

            return Result<TransactionDTO>.Success(
                TransactionMapper.ToDTO(transaction));
        }
        catch
        {
            await RollbackAndClearAsync(dbTransaction);
            throw;
        }
    }

    // --------------------------------------------------
    // VALIDAR ÂMBITO DA CONTA
    // --------------------------------------------------

    private static Result ValidateAccountScope(
        Transaction transaction,
        Account account)
    {
        if (transaction.FamilyId.HasValue)
        {
            if (account.FamilyId != transaction.FamilyId)
            {
                return Error.BusinessRule(
                    "Transaction.AccountScopeMismatch",
                    "A conta não pertence à família da transação.");
            }
        }
        else if (account.FamilyId.HasValue ||
                 account.UserId != transaction.UserId)
        {
            return Error.BusinessRule(
                "Transaction.AccountScopeMismatch",
                "A conta não pertence ao proprietário da transação.");
        }

        return Result.Success();
    }

    // --------------------------------------------------
    // VALIDAR ÂMBITO DA CATEGORIA
    // --------------------------------------------------

    private static Result ValidateCategoryScope(
        Transaction transaction,
        Category category)
    {
        if (category.IsGlobal)
            return Result.Success();

        if (transaction.FamilyId.HasValue)
        {
            if (category.FamilyId != transaction.FamilyId)
            {
                return Error.BusinessRule(
                    "Transaction.CategoryScopeMismatch",
                    "A categoria não pertence à família da transação.");
            }
        }
        else if (category.FamilyId.HasValue ||
                 category.UserId != transaction.UserId)
        {
            return Error.BusinessRule(
                "Transaction.CategoryScopeMismatch",
                "A categoria não pertence ao proprietário da transação.");
        }

        return Result.Success();
    }

    // --------------------------------------------------
    // ROLLBACK E LIMPEZA DO CHANGE TRACKER
    // --------------------------------------------------

    private async Task RollbackAndClearAsync(
        IDbTransaction dbTransaction)
    {
        try
        {
            await dbTransaction.RollbackAsync();
        }
        finally
        {
            // O rollback da base de dados não restaura
            // automaticamente as entidades rastreadas.
            _unitOfWork.ClearTrackedChanges();
        }
    }
}
