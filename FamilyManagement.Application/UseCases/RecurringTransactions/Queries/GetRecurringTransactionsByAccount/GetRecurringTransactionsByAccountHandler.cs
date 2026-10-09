using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries
    .GetRecurringTransactionsByAccount;

public sealed class GetRecurringTransactionsByAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecurringTransactionsByAccountHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<IEnumerable<RecurringTransactionDTO>>> HandleAsync(
        GetRecurringTransactionsByAccountQuery query)
    {
        if (query.AccountId == Guid.Empty)
        {
            return Error.Validation(
                "A conta é obrigatória.");
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(
            query.AccountId);

        if (account is null)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta não encontrada.");
        }

        var recurringTransactions =
            await _unitOfWork.RecurringTransactions
                .GetRecurringTransactionsByAccountIdWithDetailsAsync(
                    query.AccountId);

        var dtos = recurringTransactions
            .OrderBy(rt => rt.NextDueDate)
            .Select(RecurringTransactionMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<RecurringTransactionDTO>>
            .Success(dtos);
    }
}