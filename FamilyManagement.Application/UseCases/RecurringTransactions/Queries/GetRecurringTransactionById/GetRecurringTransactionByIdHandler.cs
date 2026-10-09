using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetRecurringTransactionById;

public sealed class GetRecurringTransactionByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecurringTransactionByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        GetRecurringTransactionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var recurringTransaction =
            await _unitOfWork.RecurringTransactions
                .GetRecurringTransactionByIdWithDetailsAsync(
                    query.RecurringTransactionId);

        if (recurringTransaction is null)
        {
            return Error.NotFound(
                "Recurring.NotFound",
                "Recorrência não encontrada.");
        }

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}