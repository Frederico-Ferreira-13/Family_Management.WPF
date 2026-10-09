using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.UpdateRecurringTransaction;

public sealed class UpdateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecurringTransactionHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        UpdateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var recurringTransaction =
            await _unitOfWork.RecurringTransactions
                .GetByIdAsync(command.RecurringTransactionId);

        if (recurringTransaction is null)
        {
            return Error.NotFound(
                "Recurring.NotFound",
                "Recorrência não encontrada.");
        }

        if (!recurringTransaction.Description.Equals(
                command.Description.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _unitOfWork.RecurringTransactions
                .RecurringTransactionExistsForUserByDescriptionAndIdAsync(
                    command.Description,
                    recurringTransaction.UserId,
                    recurringTransaction.Id);

            if (exists)
            {
                return Error.Conflict(
                    "Recurring.Duplicate",
                    "Já existe outra recorrência com esta descrição.");
            }
        }

        var account = await _unitOfWork.Accounts
            .GetByIdAsync(command.AccountId);

        if (account is null ||
            !account.IsActive ||
            account.UserId != recurringTransaction.UserId)
        {
            return Error.Validation(
                "A conta é inválida ou não pertence ao utilizador.");
        }

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.Validation(
                "A categoria selecionada é inválida.");
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            recurringTransaction.Amount.Currency);

        if (moneyResult.IsFailure)
            return moneyResult.Error;

        if (account.Balance.Currency !=
            recurringTransaction.Amount.Currency)
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        var updateResult = recurringTransaction.UpdateDetails(
            description: command.Description,
            amount: moneyResult.Value,
            type: command.Type,
            frequency: command.Frequency,
            categoryId: command.CategoryId,
            accountId: command.AccountId,
            endDate: command.EndDate,
            isActive: recurringTransaction.IsActive);

        if (updateResult.IsFailure)
            return updateResult.Error;

        _unitOfWork.RecurringTransactions.Update(
            recurringTransaction);

        await _unitOfWork.CompleteAsync();

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}