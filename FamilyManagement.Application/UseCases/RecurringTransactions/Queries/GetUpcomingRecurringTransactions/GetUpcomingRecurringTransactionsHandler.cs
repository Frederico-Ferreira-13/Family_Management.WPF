using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions
    .Queries.GetUpcomingRecurringTransactions;

public sealed class GetUpcomingRecurringTransactionsHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUpcomingRecurringTransactionsHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<RecurringTransactionDTO>>>
        HandleAsync(
            GetUpcomingRecurringTransactionsQuery query,
            CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (query.DaysLookahead < 0)
        {
            return Error.Validation(
                "O número de dias de antecedência não pode ser negativo.");
        }

        var family = await _unitOfWork.Families
            .GetByIdAsync(query.FamilyId);

        if (family is null)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        var today = DateTime.UtcNow.Date;
        var limitDate = today.AddDays(query.DaysLookahead);

        var recurringTransactions =
            await _unitOfWork.RecurringTransactions
                .FindRecurringTransactionsWithDetailsAsync(rt =>
                    rt.User != null &&
                    rt.User.FamilyId == query.FamilyId &&
                    rt.IsActive &&
                    rt.NextDueDate >= today &&
                    rt.NextDueDate <= limitDate);

        var result = recurringTransactions
            .OrderBy(rt => rt.NextDueDate)
            .ThenBy(rt => rt.Description)
            .Select(RecurringTransactionMapper.ToDTO)
            .ToList();

        return result;
    }
}