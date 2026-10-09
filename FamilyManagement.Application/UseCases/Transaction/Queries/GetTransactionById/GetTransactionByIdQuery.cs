namespace FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionById;

public sealed record GetTransactionByIdQuery(
    Guid TransactionId);