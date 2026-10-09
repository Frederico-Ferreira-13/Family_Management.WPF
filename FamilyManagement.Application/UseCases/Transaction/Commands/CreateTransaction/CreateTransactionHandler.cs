using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransaction;

public sealed class CreateTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public CreateTransactionHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _impactService = impactService
            ?? throw new ArgumentNullException(nameof(impactService));
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        CreateTransactionCommand command)
    {
        if (command.UserId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        if (command.AccountId == Guid.Empty)
            return Error.Validation("A conta é obrigatória.");

        if (command.CategoryId == Guid.Empty)
            return Error.Validation("A categoria é obrigatória.");

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        var account = await _unitOfWork.Accounts
            .GetByIdAsync(command.AccountId);

        if (account is null || !account.IsActive)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta não encontrada.");
        }

        if (account.UserId != command.UserId)
        {
            return Error.BusinessRule(
                "Transaction.AccountOwnerMismatch",
                "A conta não pertence ao utilizador.");
        }

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.NotFound(
                "Category.NotFound",
                "Categoria não encontrada.");
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
            return moneyResult.Error;

        var money = moneyResult.Value;

        if (!string.Equals(
                account.Balance.Currency,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        var transactionResult =
            Transaction.CreateIncomeOrExpense(
                command.Date,
                money,
                command.Description,
                command.Type,
                command.UserId,
                command.AccountId,
                command.CategoryId,
                command.Notes,
                command.BudgetId,
                command.GoalId,
                command.IsConfirmed);

        if (transactionResult.IsFailure)
            return transactionResult.Error;

        var transaction = transactionResult.Value;

        using var databaseTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (transaction.IsConfirmed)
            {
                var impactResult =
                    await _impactService.ApplyAsync(transaction);

                if (impactResult.IsFailure)
                {
                    await databaseTransaction.RollbackAsync();
                    return impactResult.Error;
                }
            }

            await _unitOfWork.Transactions.AddAsync(transaction);

            await _unitOfWork.CompleteAsync();

            await databaseTransaction.CommitAsync();

            return TransactionMapper.ToDTO(transaction);
        }
        catch
        {
            await databaseTransaction.RollbackAsync();
            throw;
        }
    }
}