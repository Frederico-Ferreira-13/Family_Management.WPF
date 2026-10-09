using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionsByUser;

public sealed class GetTransactionsByUserHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsByUserHandler(
        ITransactionRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<IEnumerable<TransactionDTO>>> HandleAsync(
        GetTransactionsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var transactions =
            await _repository.GetByUserIdWithDetailsAsync(
                query.UserId,
                query.Filter);

        cancellationToken.ThrowIfCancellationRequested();

        var result = transactions
            .OrderByDescending(transaction => transaction.Date)
            .Select(TransactionMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<TransactionDTO>>
            .Success(result);
    }
}