using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands
    .GeneratePendingRecurringTransactions;

public sealed class GeneratePendingRecurringTransactionsHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GeneratePendingRecurringTransactionsHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> HandleAsync(
        GeneratePendingRecurringTransactionsCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var asOfDate = (command.AsOfDate ?? DateTime.Today).Date;

        var recurringTransactions =
            await _unitOfWork.RecurringTransactions
                .GetActiveRecurringTransactionsByUserIdAsync(
                    command.UserId);

        var generatedCount = 0;

        foreach (var recurring in recurringTransactions)
        {
            while (ShouldGenerate(recurring, asOfDate))
            {
                var transactionResult =
                    recurring.GenerateTransaction(
                        isConfirmed: false,
                        referenceDate: asOfDate);

                if (transactionResult.IsFailure)
                    return transactionResult.Error;

                var transaction = transactionResult.Value;

                var recordResult =
                    recurring.RecordGeneratedTransaction(
                        transaction,
                        referenceDate: asOfDate);

                if (recordResult.IsFailure)
                    return recordResult.Error;

                await _unitOfWork.Transactions
                    .AddAsync(transaction);

                generatedCount++;
            }
        }

        await _unitOfWork.CompleteAsync();

        return Result<int>.Success(generatedCount);
    }

    private static bool ShouldGenerate(
        RecurringTransaction recurring,
        DateTime asOfDate)
    {
        if (!recurring.IsActive)
            return false;

        if (recurring.NextDueDate.Date > asOfDate)
            return false;

        if (recurring.EndDate.HasValue &&
            recurring.NextDueDate.Date >
            recurring.EndDate.Value.Date)
        {
            return false;
        }

        return true;
    }
}