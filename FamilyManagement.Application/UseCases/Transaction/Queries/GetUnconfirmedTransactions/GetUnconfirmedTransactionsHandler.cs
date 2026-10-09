using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetUnconfirmedTransactions;

public sealed class GetUnconfirmedTransactionsHandler
{
    private readonly ITransactionRepository _repository;

    public GetUnconfirmedTransactionsHandler(
        ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TransactionDTO>>> HandleAsync(
        GetUnconfirmedTransactionsQuery query)
    {
        var transactions =
            await _repository.GetUnconfirmedAsync(
                query.UserId);

        var result = transactions
            .OrderBy(x => x.Date)
            .Select(TransactionMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<TransactionDTO>>.Success(result);
    }
}