using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionById;

public sealed class GetTransactionByIdHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdHandler(
        ITransactionRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        GetTransactionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.TransactionId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da transação é obrigatório.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var transaction =
            await _repository.GetByIdWithDetailsAsync(
                query.TransactionId);

        cancellationToken.ThrowIfCancellationRequested();

        if (transaction is null)
        {
            return Error.NotFound(
                "Transaction.NotFound",
                "Transação não encontrada.");
        }

        return TransactionMapper.ToDTO(transaction);
    }
}