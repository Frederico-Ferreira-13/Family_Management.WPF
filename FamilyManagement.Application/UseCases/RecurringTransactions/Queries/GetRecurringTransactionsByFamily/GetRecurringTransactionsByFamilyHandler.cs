using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.RecurringTransactions
    .Queries.GetRecurringTransactionsByFamily;

public sealed class GetRecurringTransactionsByFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecurringTransactionsByFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<RecurringTransactionDTO>>>
        HandleAsync(
            GetRecurringTransactionsByFamilyQuery query,
            CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        var family = await _unitOfWork.Families
            .GetByIdAsync(query.FamilyId);

        if (family is null)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        IEnumerable<RecurringTransaction> recurringTransactions;

        if (query.OnlyActive)
        {
            recurringTransactions =
                await _unitOfWork.RecurringTransactions
                    .FindRecurringTransactionsWithDetailsAsync(rt =>
                        rt.User != null &&
                        rt.User.FamilyId == query.FamilyId &&
                        rt.IsActive);
        }
        else
        {
            recurringTransactions =
                await _unitOfWork.RecurringTransactions
                    .FindRecurringTransactionsWithDetailsAsync(rt =>
                        rt.User != null &&
                        rt.User.FamilyId == query.FamilyId);
        }

        var result = recurringTransactions
            .OrderBy(rt => rt.NextDueDate)
            .ThenBy(rt => rt.Description)
            .Select(RecurringTransactionMapper.ToDTO)
            .ToList();

        return result;
    }
}