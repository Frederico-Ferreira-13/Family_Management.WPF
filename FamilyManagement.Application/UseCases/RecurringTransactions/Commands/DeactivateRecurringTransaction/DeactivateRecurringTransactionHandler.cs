using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.DeactivateRecurringTransaction;

public sealed class DeactivateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateRecurringTransactionHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        DeactivateRecurringTransactionCommand command,
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

        if (!recurringTransaction.IsActive)
        {
            return Result.Success();
        }

        recurringTransaction.Deactivate();

        _unitOfWork.RecurringTransactions.Update(
            recurringTransaction);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}