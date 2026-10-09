namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetTransactionsByCategory;

public sealed record GetTransactionsByCategoryQuery(
    Guid CategoryId);