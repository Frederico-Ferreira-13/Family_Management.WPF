namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetUnconfirmedTransactions;

public sealed record GetUnconfirmedTransactionsQuery(
    Guid UserId);