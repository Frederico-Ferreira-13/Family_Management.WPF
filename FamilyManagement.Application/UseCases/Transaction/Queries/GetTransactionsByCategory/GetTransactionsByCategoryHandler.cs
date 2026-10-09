using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetTransactionsByCategory;

public sealed class GetTransactionsByCategoryHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsByCategoryHandler(
        ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TransactionDTO>>> HandleAsync(
        GetTransactionsByCategoryQuery query)
    {
        var transactions =
            await _repository.FindAsync(x =>
                x.CategoryId == query.CategoryId &&
                x.IsActive);

        var result = transactions
            .OrderByDescending(x => x.Date)
            .Select(TransactionMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<TransactionDTO>>.Success(result);
    }
}