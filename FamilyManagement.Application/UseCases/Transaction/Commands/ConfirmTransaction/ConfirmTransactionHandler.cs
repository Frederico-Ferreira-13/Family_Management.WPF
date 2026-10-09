using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.ConfirmTransaction;

public sealed class ConfirmTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public ConfirmTransactionHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork;
        _impactService = impactService;
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        ConfirmTransactionCommand command)
    {
        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(command.TransactionId);

        if (transaction is null)
            return Error.NotFound(
                "Transaction.NotFound",
                "Transação não encontrada.");

        if (transaction.IsConfirmed)
            return TransactionMapper.ToDTO(transaction);

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            var confirm = transaction.Confirm();

            if (confirm.IsFailure)
            {
                await dbTransaction.RollbackAsync();
                return confirm.Error;
            }

            var impact = await _impactService.ApplyAsync(transaction);

            if (impact.IsFailure)
            {
                await dbTransaction.RollbackAsync();
                return impact.Error;
            }

            _unitOfWork.Transactions.Update(transaction);

            await _unitOfWork.CompleteAsync();
            await dbTransaction.CommitAsync();

            return TransactionMapper.ToDTO(transaction);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}