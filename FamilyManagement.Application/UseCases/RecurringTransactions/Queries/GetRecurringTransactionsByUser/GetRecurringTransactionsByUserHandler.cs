using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetRecurringTransactionsByUser;

public sealed class GetRecurringTransactionsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecurringTransactionsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<RecurringTransactionDTO>>> HandleAsync(
        GetRecurringTransactionsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var recurringTransactions =
            await _unitOfWork.RecurringTransactions
                .GetRecurringTransactionsByUserIdWithDetailsAsync(
                    query.UserId);

        return recurringTransactions
            .OrderBy(item => item.NextDueDate)
            .Select(RecurringTransactionMapper.ToDTO)
            .ToList();
    }
}