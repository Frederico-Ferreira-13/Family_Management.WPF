
using System;
using System.Threading.Tasks;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Transactions.Services;

public sealed class TransactionImpactService : ITransactionImpactService
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionImpactService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> ApplyAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (!transaction.IsActive || !transaction.IsConfirmed)
        {
            return Error.BusinessRule(
                "Transaction.NotApplicable",
                "A transação deve estar ativa e confirmada.");
        }

        // As transferências exigem sempre uma conta de origem.
        // As receitas e despesas podem não ter conta bancária,
        // desde que estejam associadas a um plano financeiro.
        if (!transaction.AccountId.HasValue &&
            (transaction.Type == TransactionType.Transfer ||
             !transaction.FinancialPlanId.HasValue))
        {
            return Error.Validation(
                "A transação necessita de uma conta bancária ou de um plano financeiro válido.");
        }

        Account? account = null;

        if (transaction.AccountId.HasValue)
        {
            account = await _unitOfWork.Accounts
                .GetByIdAsync(transaction.AccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "Conta não encontrada ou inativa.");
            }

            var scopeResult = ValidateAccountScope(
                transaction,
                account);

            if (scopeResult.IsFailure)
                return scopeResult;

            if (account.Balance.Currency != transaction.Amount.Currency)
                return DomainErrors.Money.CurrencyMismatch;
        }

        return transaction.Type switch
        {
            TransactionType.Income =>
                ApplyIncome(transaction, account),

            TransactionType.Expense =>
                await ApplyExpenseAsync(transaction, account),

            TransactionType.Transfer when account is not null =>
                await ApplyTransferAsync(transaction, account),

            TransactionType.Transfer =>
                Error.Validation(
                    "A conta de origem é obrigatória numa transferência."),

            _ => Error.Validation(
                "Tipo de transação inválido.")
        };
    }

    public async Task<Result> RevertAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (!transaction.IsConfirmed)
            return Result.Success();

        // Não permitir transferências sem conta de origem.
        if (!transaction.AccountId.HasValue &&
            transaction.Type == TransactionType.Transfer)
        {
            return Error.Validation(
                "A conta de origem é obrigatória numa transferência.");
        }

        Account? account = null;

        if (transaction.AccountId.HasValue)
        {
            account = await _unitOfWork.Accounts
                .GetByIdAsync(transaction.AccountId.Value);

            // Na reversão, uma conta inativa pode continuar
            // a necessitar de correção do seu saldo histórico.
            if (account is null)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "Conta não encontrada.");
            }

            var scopeResult = ValidateAccountScope(
                transaction,
                account);

            if (scopeResult.IsFailure)
                return scopeResult;

            if (account.Balance.Currency != transaction.Amount.Currency)
                return DomainErrors.Money.CurrencyMismatch;
        }

        return transaction.Type switch
        {
            TransactionType.Income =>
                RevertIncome(transaction, account),

            TransactionType.Expense =>
                await RevertExpenseAsync(transaction, account),

            TransactionType.Transfer when account is not null =>
                await RevertTransferAsync(transaction, account),

            TransactionType.Transfer =>
                Error.Validation(
                    "A conta de origem é obrigatória numa transferência."),

            _ => Error.Validation(
                "Tipo de transação inválido.")
        };
    }

    // --------------------------------------------------
    // RECEITAS
    // --------------------------------------------------

    private static Result ApplyIncome(
        Transaction transaction,
        Account? account)
    {
        // Uma receita sem conta bancária pertence ao plano
        // financeiro, mas não altera qualquer saldo bancário.
        if (account is null)
            return Result.Success();

        return account.Deposit(transaction.Amount);
    }

    private static Result RevertIncome(
        Transaction transaction,
        Account? account)
    {
        if (account is null)
            return Result.Success();

        return account.Withdraw(transaction.Amount);
    }

    // --------------------------------------------------
    // DESPESAS - APLICAÇÃO
    // --------------------------------------------------

    private async Task<Result> ApplyExpenseAsync(
        Transaction transaction,
        Account? account)
    {
        Budget? budget = null;
        Goal? goal = null;
        GoalContributionImpact? existingImpact = null;

        // Validar orçamento.
        if (transaction.BudgetId.HasValue)
        {
            budget = await _unitOfWork.Budgets
                .GetByIdAsync(transaction.BudgetId.Value);

            if (budget is null || !budget.IsActive)
            {
                return Error.NotFound(
                    "Budget.NotFound",
                    "Orçamento não encontrado ou inativo.");
            }

            var validation = ValidateBudgetScope(
                transaction,
                budget);

            if (validation.IsFailure)
                return validation;
        }

        // Validar meta financeira.
        if (transaction.GoalId.HasValue)
        {
            goal = await _unitOfWork.Goals
                .GetByIdAsync(transaction.GoalId.Value);

            if (goal is null || !goal.IsActive)
            {
                return Error.NotFound(
                    "Goal.NotFound",
                    "Meta não encontrada ou inativa.");
            }

            var validation = ValidateGoalScope(
                transaction,
                goal);

            if (validation.IsFailure)
                return validation;

            existingImpact =
                await _unitOfWork.GoalContributionImpacts
                    .GetByTransactionIdAsync(transaction.Id);

            if (existingImpact is not null &&
                existingImpact.IsApplied)
            {
                return Error.Conflict(
                    "GoalContribution.AlreadyApplied",
                    "A contribuição desta transação já foi aplicada.");
            }
        }

        // Todas as referências foram validadas.
        // Apenas retirar dinheiro da conta se existir.
        if (account is not null)
        {
            var withdrawResult =
                account.Withdraw(transaction.Amount);

            if (withdrawResult.IsFailure)
                return withdrawResult;
        }

        // Atualizar o orçamento, mesmo quando
        // a transação não possui conta bancária.
        if (budget is not null)
        {
            var budgetResult =
                budget.AddExpense(transaction.Amount);

            if (budgetResult.IsFailure)
                return budgetResult;
        }

        // Aplicar contribuição para a meta financeira.
        if (goal is not null)
        {
            var depositResult =
                goal.Deposit(transaction.Amount);

            if (depositResult.IsFailure)
                return depositResult;

            if (existingImpact is null)
            {
                var createResult =
                    GoalContributionImpact.Create(
                        transaction.Id,
                        goal.Id,
                        transaction.Amount);

                if (createResult.IsFailure)
                    return createResult.Error;

                await _unitOfWork.GoalContributionImpacts
                    .AddAsync(createResult.Value);
            }
            else
            {
                var reapplyResult =
                    existingImpact.Reapply(
                        goal.Id,
                        transaction.Amount);

                if (reapplyResult.IsFailure)
                    return reapplyResult;
            }
        }

        return Result.Success();
    }

    // --------------------------------------------------
    // DESPESAS - REVERSÃO
    // --------------------------------------------------

    private async Task<Result> RevertExpenseAsync(
        Transaction transaction,
        Account? account)
    {
        Budget? budget = null;
        Goal? goal = null;
        GoalContributionImpact? impact = null;

        if (transaction.BudgetId.HasValue)
        {
            budget = await _unitOfWork.Budgets
                .GetByIdAsync(transaction.BudgetId.Value);

            if (budget is null)
            {
                return Error.NotFound(
                    "Budget.NotFound",
                    "Orçamento associado não encontrado.");
            }

            var validation = ValidateBudgetScope(
                transaction,
                budget);

            if (validation.IsFailure)
                return validation;
        }

        if (transaction.GoalId.HasValue)
        {
            impact = await _unitOfWork.GoalContributionImpacts
                .GetByTransactionIdAsync(transaction.Id);

            // Nunca presumir que a ausência de histórico
            // significa que a contribuição não foi aplicada.
            if (impact is null)
            {
                return Error.Conflict(
                    "GoalContribution.MissingHistory",
                    "Não existe histórico de impacto para esta contribuição. " +
                    "É necessária reconciliação antes da reversão.");
            }

            if (!impact.IsApplied)
            {
                return Error.Conflict(
                    "GoalContribution.AlreadyReverted",
                    "A contribuição já foi revertida.");
            }

            goal = await _unitOfWork.Goals
                .GetByIdAsync(impact.GoalId);

            if (goal is null)
            {
                return Error.NotFound(
                    "Goal.NotFound",
                    "Meta associada ao impacto não encontrada.");
            }

            var validation = ValidateGoalScope(
                transaction,
                goal);

            if (validation.IsFailure)
                return validation;

            if (impact.Currency != goal.TargetAmount.Currency)
                return DomainErrors.Money.CurrencyMismatch;
        }

        // Reverter a contribuição usando o montante
        // efetivamente aplicado, preservado no histórico.
        if (goal is not null && impact is not null)
        {
            var appliedAmount = Money.Create(
                impact.AppliedAmount,
                impact.Currency);

            var goalResult =
                goal.Withdraw(appliedAmount);

            if (goalResult.IsFailure)
                return goalResult;

            var markResult =
                impact.MarkReverted();

            if (markResult.IsFailure)
                return markResult;
        }

        // Reverter o impacto no orçamento.
        if (budget is not null)
        {
            var budgetResult =
                budget.RemoveExpense(transaction.Amount);

            if (budgetResult.IsFailure)
                return budgetResult;
        }

        // Repor o saldo apenas quando existe
        // uma conta bancária associada.
        if (account is not null)
        {
            return account.Deposit(transaction.Amount);
        }

        return Result.Success();
    }

    // --------------------------------------------------
    // TRANSFERÊNCIAS - APLICAÇÃO
    // --------------------------------------------------

    private async Task<Result> ApplyTransferAsync(
        Transaction transaction,
        Account sourceAccount)
    {
        if (!transaction.TargetAccountId.HasValue)
        {
            return Error.Validation(
                "A conta de destino é obrigatória.");
        }

        var targetAccount = await _unitOfWork.Accounts
            .GetByIdAsync(transaction.TargetAccountId.Value);

        if (targetAccount is null || !targetAccount.IsActive)
        {
            return Error.NotFound(
                "Account.TargetNotFound",
                "Conta de destino não encontrada ou inativa.");
        }

        var validation = ValidateAccountScope(
            transaction,
            targetAccount);

        if (validation.IsFailure)
            return validation;

        if (sourceAccount.Id == targetAccount.Id)
        {
            return Error.Validation(
                "As contas devem ser diferentes.");
        }

        if (targetAccount.Balance.Currency != transaction.Amount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return sourceAccount.TransferTo(
            targetAccount,
            transaction.Amount);
    }

    // --------------------------------------------------
    // TRANSFERÊNCIAS - REVERSÃO
    // --------------------------------------------------

    private async Task<Result> RevertTransferAsync(
        Transaction transaction,
        Account sourceAccount)
    {
        if (!transaction.TargetAccountId.HasValue)
        {
            return Error.Validation(
                "A conta de destino é obrigatória.");
        }

        var targetAccount = await _unitOfWork.Accounts
            .GetByIdAsync(transaction.TargetAccountId.Value);

        if (targetAccount is null)
        {
            return Error.NotFound(
                "Account.TargetNotFound",
                "Conta de destino não encontrada.");
        }

        var validation = ValidateAccountScope(
            transaction,
            targetAccount);

        if (validation.IsFailure)
            return validation;

        if (sourceAccount.Id == targetAccount.Id)
        {
            return Error.Validation(
                "As contas devem ser diferentes.");
        }

        if (targetAccount.Balance.Currency != transaction.Amount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return targetAccount.TransferTo(
            sourceAccount,
            transaction.Amount);
    }

    // --------------------------------------------------
    // VALIDAÇÃO DO ÂMBITO DA CONTA
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
    // VALIDAÇÃO DO ÂMBITO DO ORÇAMENTO
    // --------------------------------------------------

    private static Result ValidateBudgetScope(
        Transaction transaction,
        Budget budget)
    {
        if (budget.FamilyId != transaction.FamilyId ||
            (!transaction.FamilyId.HasValue &&
             budget.UserId != transaction.UserId))
        {
            return Error.BusinessRule(
                "Transaction.BudgetScopeMismatch",
                "O orçamento não pertence ao âmbito da transação.");
        }

        if (budget.CategoryId != transaction.CategoryId)
        {
            return Error.BusinessRule(
                "Transaction.BudgetCategoryMismatch",
                "A categoria do orçamento é incompatível.");
        }

        if (budget.Month != transaction.Date.Month ||
            budget.Year != transaction.Date.Year)
        {
            return Error.BusinessRule(
                "Transaction.BudgetPeriodMismatch",
                "O orçamento não corresponde ao período da transação.");
        }

        if (budget.BudgetedAmount.Currency != transaction.Amount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return Result.Success();
    }

    // --------------------------------------------------
    // VALIDAÇÃO DO ÂMBITO DA META
    // --------------------------------------------------

    private static Result ValidateGoalScope(
        Transaction transaction,
        Goal goal)
    {
        if (goal.FamilyId != transaction.FamilyId ||
            (!transaction.FamilyId.HasValue &&
             goal.UserId != transaction.UserId))
        {
            return Error.BusinessRule(
                "Transaction.GoalScopeMismatch",
                "A meta não pertence ao âmbito da transação.");
        }

        if (goal.TargetAmount.Currency != transaction.Amount.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        return Result.Success();
    }
}
