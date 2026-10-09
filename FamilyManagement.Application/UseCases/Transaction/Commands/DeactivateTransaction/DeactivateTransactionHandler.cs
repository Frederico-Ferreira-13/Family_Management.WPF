using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.DeactivateTransaction;

public sealed class DeactivateTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public DeactivateTransactionHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork;
        _impactService = impactService;
    }

    public async Task<Result> HandleAsync(
        DeactivateTransactionCommand command)
    {
        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(command.TransactionId);

        if (transaction is null)
            return Error.NotFound(
                "Transaction.NotFound",
                "Transação não encontrada.");

        if (!transaction.IsActive)
            return Result.Success();

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (transaction.IsConfirmed)
            {
                var revert = await _impactService.RevertAsync(transaction);

                if (revert.IsFailure)
                {
                    await dbTransaction.RollbackAsync();
                    return revert.Error;
                }
            }

            transaction.Deactivate();

            _unitOfWork.Transactions.Update(transaction);

            await _unitOfWork.CompleteAsync();
            await dbTransaction.CommitAsync();

            return Result.Success();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}