using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionsByUser;

public sealed record GetTransactionsByUserQuery(
    Guid UserId,
    TransactionFilter? Filter = null);