using System;

using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

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

        if (!transaction.IsActive)
        {
            return Error.Conflict(
                "Transaction.Inactive",
                "Não é possível aplicar uma transação inativa.");
        }

        if (!transaction.IsConfirmed)
        {
            return Error.BusinessRule(
                "Transaction.NotConfirmed",
                "Apenas transações confirmadas podem produzir impacto financeiro.");
        }

        var account = await _unitOfWork.Accounts
            .GetByIdAsync(transaction.AccountId);

        if (account is null || !account.IsActive)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta de origem não encontrada.");
        }

        return transaction.Type switch
        {
            TransactionType.Income =>
                account.Deposit(transaction.Amount),

            TransactionType.Expense =>
                await ApplyExpenseAsync(transaction, account),

            TransactionType.Transfer =>
                await ApplyTransferAsync(transaction, account),

            _ => Error.Validation(
                "O tipo da transação não é válido.")
        };
    }

    public async Task<Result> RevertAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (!transaction.IsConfirmed)
            return Result.Success();

        var account = await _unitOfWork.Accounts
            .GetByIdAsync(transaction.AccountId);

        if (account is null)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta de origem não encontrada.");
        }

        return transaction.Type switch
        {
            TransactionType.Income =>
                account.Withdraw(transaction.Amount),

            TransactionType.Expense =>
                await RevertExpenseAsync(transaction, account),

            TransactionType.Transfer =>
                await RevertTransferAsync(transaction, account),

            _ => Error.Validation(
                "O tipo da transação não é válido.")
        };
    }

    private async Task<Result> ApplyExpenseAsync(
        Transaction transaction,
        Account account)
    {
        Budget? budget = null;
        Goal? goal = null;

        // Validar primeiro o orçamento.
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
        }

        // Validar primeiro o objetivo.
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
        }

        // Só agora iniciamos as alterações financeiras.
        var withdrawResult = account.Withdraw(transaction.Amount);

        if (withdrawResult.IsFailure)
            return withdrawResult.Error;

        if (budget is not null)
        {
            var budgetResult = budget.AddExpense(transaction.Amount);

            if (budgetResult.IsFailure)
                return budgetResult.Error;
        }

        if (goal is not null)
        {
            var goalResult = goal.AddContribution(transaction);

            if (goalResult.IsFailure)
                return goalResult.Error;
        }

        return Result.Success();
    }

    private async Task<Result> ApplyTransferAsync(
        Transaction transaction,
        Account sourceAccount)
    {
        if (!transaction.TargetAccountId.HasValue)
        {
            return Error.Validation(
                "A conta de destino é obrigatória para uma transferência.");
        }

        var targetAccount = await _unitOfWork.Accounts
            .GetByIdAsync(transaction.TargetAccountId.Value);

        if (targetAccount is null || !targetAccount.IsActive)
        {
            return Error.NotFound(
                "Account.TargetNotFound",
                "Conta de destino não encontrada ou inativa.");
        }

        return sourceAccount.TransferTo(
            targetAccount,
            transaction.Amount);
    }

    private async Task<Result> RevertExpenseAsync(
        Transaction transaction,
        Account account)
    {
        Budget? budget = null;
        Goal? goal = null;

        // Carregar todas as entidades antes da reversão.
        if (transaction.BudgetId.HasValue)
        {
            budget = await _unitOfWork.Budgets
                .GetByIdAsync(transaction.BudgetId.Value);

            if (budget is null)
            {
                return Error.NotFound(
                    "Budget.NotFound",
                    "Orçamento associado à transação não encontrado.");
            }
        }

        if (transaction.GoalId.HasValue)
        {
            goal = await _unitOfWork.Goals
                .GetByIdAsync(transaction.GoalId.Value);

            if (goal is null)
            {
                return Error.NotFound(
                    "Goal.NotFound",
                    "Meta associada à transação não encontrada.");
            }
        }

        // Reverter as associações financeiras.
        if (goal is not null)
        {
            var goalResult = goal.Withdraw(transaction.Amount);

            if (goalResult.IsFailure)
                return goalResult.Error;
        }

        if (budget is not null)
        {
            var budgetResult = budget.RemoveExpense(transaction.Amount);

            if (budgetResult.IsFailure)
                return budgetResult.Error;
        }

        return account.Deposit(transaction.Amount);
    }

    private async Task<Result> RevertTransferAsync(
        Transaction transaction,
        Account sourceAccount)
    {
        if (!transaction.TargetAccountId.HasValue)
        {
            return Error.Validation(
                "A transferência não possui uma conta de destino.");
        }

        var targetAccount = await _unitOfWork.Accounts
            .GetByIdAsync(transaction.TargetAccountId.Value);

        if (targetAccount is null)
        {
            return Error.NotFound(
                "Account.TargetNotFound",
                "Conta de destino não encontrada.");
        }

        return targetAccount.TransferTo(
            sourceAccount,
            transaction.Amount);
    }
}