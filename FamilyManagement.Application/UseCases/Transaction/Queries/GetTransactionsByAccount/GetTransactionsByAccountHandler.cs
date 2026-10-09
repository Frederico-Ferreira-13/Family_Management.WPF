using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetTransactionsByAccount;

public sealed class GetTransactionsByAccountHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsByAccountHandler(
        ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TransactionDTO>>> HandleAsync(
        GetTransactionsByAccountQuery query)
    {
        var transactions =
            await _repository.GetByAccountIdAsync(
                query.AccountId);

        var result = transactions
            .OrderByDescending(x => x.Date)
            .Select(TransactionMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<TransactionDTO>>.Success(result);
    }
}