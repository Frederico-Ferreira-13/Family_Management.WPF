using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetActiveRecurringTransactionsByUser;

public sealed class GetActiveRecurringTransactionsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveRecurringTransactionsByUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<RecurringTransactionDTO>>> HandleAsync(
        GetActiveRecurringTransactionsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var recurringTransactions =
            await _unitOfWork.RecurringTransactions
                .GetRecurringTransactionsByUserIdWithDetailsAsync(
                    query.UserId);

        return recurringTransactions
            .Where(item => item.IsActive)
            .OrderBy(item => item.NextDueDate)
            .Select(RecurringTransactionMapper.ToDTO)
            .ToList();
    }
}