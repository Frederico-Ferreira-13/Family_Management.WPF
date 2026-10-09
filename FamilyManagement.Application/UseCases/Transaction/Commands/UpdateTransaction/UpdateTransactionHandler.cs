using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
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
        if (command.TransactionId == Guid.Empty)
        {
            return Error.Validation(
                "A transação é obrigatória.");
        }

        var transaction =
            await _unitOfWork.Transactions.GetByIdAsync(
                command.TransactionId);

        if (transaction is null ||
            !transaction.IsActive)
        {
            return Error.NotFound(
                "Transaction.NotFound",
                "Transação não encontrada.");
        }

        Money? newAmount = null;

        if (command.Amount.HasValue)
        {
            var moneyResult = Money.TryCreate(
                command.Amount.Value,
                transaction.Amount.Currency);

            if (moneyResult.IsFailure)
            {
                return moneyResult.Error;
            }

            newAmount = moneyResult.Value;
        }

        if (command.AccountId.HasValue)
        {
            if (command.AccountId.Value == Guid.Empty)
            {
                return Error.Validation(
                    "A conta é inválida.");
            }

            var account =
                await _unitOfWork.Accounts.GetByIdAsync(
                    command.AccountId.Value);

            if (account is null ||
                !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "Conta não encontrada.");
            }

            if (account.UserId != transaction.UserId)
            {
                return Error.BusinessRule(
                    "Transaction.AccountOwnerMismatch",
                    "A conta não pertence ao utilizador da transação.");
            }

            if (!string.Equals(
                    account.Balance.Currency,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }

            if (transaction.Type ==
                    FamilyManagement.Domain.Enums.TransactionType.Transfer &&
                transaction.TargetAccountId.HasValue &&
                account.Id ==
                    transaction.TargetAccountId.Value)
            {
                return Error.Validation(
                    "As contas de origem e destino devem ser diferentes.");
            }
        }

        if (command.CategoryId.HasValue)
        {
            if (command.CategoryId.Value == Guid.Empty)
            {
                return Error.Validation(
                    "A categoria é inválida.");
            }

            var category =
                await _unitOfWork.Categories.GetByIdAsync(
                    command.CategoryId.Value);

            if (category is null ||
                !category.IsActive)
            {
                return Error.NotFound(
                    "Category.NotFound",
                    "Categoria não encontrada.");
            }

            if (transaction.Type ==
                FamilyManagement.Domain.Enums.TransactionType.Transfer)
            {
                return Error.Validation(
                    "Uma transferência não utiliza categoria.");
            }
        }

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (transaction.IsConfirmed)
            {
                var revert =
                    await _impactService.RevertAsync(
                        transaction);

                if (revert.IsFailure)
                {
                    await dbTransaction.RollbackAsync();

                    return revert.Error;
                }
            }

            var update =
                transaction.UpdateDetails(
                    command.Description,
                    command.Notes,
                    command.CategoryId,
                    newAmount,
                    command.AccountId,
                    isConfirmed: null);

            if (update.IsFailure)
            {
                await dbTransaction.RollbackAsync();

                return update.Error;
            }

            if (transaction.IsConfirmed)
            {
                var apply =
                    await _impactService.ApplyAsync(
                        transaction);

                if (apply.IsFailure)
                {
                    await dbTransaction.RollbackAsync();

                    return apply.Error;
                }
            }

            _unitOfWork.Transactions.Update(
                transaction);

            await _unitOfWork.CompleteAsync();

            await dbTransaction.CommitAsync();

            return TransactionMapper.ToDTO(
                transaction);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}