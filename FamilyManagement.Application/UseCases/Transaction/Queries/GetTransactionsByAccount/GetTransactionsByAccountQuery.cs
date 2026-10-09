namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetTransactionsByAccount;

public sealed record GetTransactionsByAccountQuery(
    Guid AccountId);