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
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        CreateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        var account = await _unitOfWork.Accounts
            .GetByIdAsync(command.AccountId);

        if (account is null ||
            !account.IsActive ||
            account.UserId != command.UserId)
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

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
            return moneyResult.Error;

        if (moneyResult.Value.Currency != account.Balance.Currency)
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        var recurringResult = RecurringTransaction.Create(
            description: command.Description,
            amount: moneyResult.Value,
            type: command.Type,
            frequency: command.Frequency,
            startDate: command.StartDate,
            categoryId: command.CategoryId,
            accountId: command.AccountId,
            userId: command.UserId,
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