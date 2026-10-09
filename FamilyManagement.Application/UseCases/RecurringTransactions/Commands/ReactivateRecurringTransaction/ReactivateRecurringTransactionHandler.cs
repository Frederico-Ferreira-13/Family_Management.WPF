using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.ReactivateRecurringTransaction;

public sealed class ReactivateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateRecurringTransactionHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        ReactivateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RecurringTransactionId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da recorrência é obrigatório.");
        }

        var recurringTransaction =
            await _unitOfWork.RecurringTransactions
                .GetByIdAsync(
                    command.RecurringTransactionId);

        if (recurringTransaction is null)
        {
            return Error.NotFound(
                "Recurring.NotFound",
                "Recorrência não encontrada.");
        }

        var result =
            recurringTransaction.Reactivate();

        if (result.IsFailure)
        {
            return result.Error;
        }

        _unitOfWork.RecurringTransactions.Update(
            recurringTransaction);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}