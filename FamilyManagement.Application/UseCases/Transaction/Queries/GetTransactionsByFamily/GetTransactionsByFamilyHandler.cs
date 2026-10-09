using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetTransactionsByFamily;

public sealed class GetTransactionsByFamilyHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsByFamilyHandler(
        ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TransactionDTO>>> HandleAsync(
        GetTransactionsByFamilyQuery query)
    {
        if (query.StartDate.Date > query.EndDate.Date)
            return Error.Validation(
                "A data inicial não pode ser posterior à data final.");

        var transactions =
            await _repository.GetByFamilyIdWithDetailsAsync(
                query.FamilyId,
                query.StartDate,
                query.EndDate);

        var result = transactions
            .OrderByDescending(x => x.Date)
            .Select(TransactionMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<TransactionDTO>>.Success(result);
    }
}