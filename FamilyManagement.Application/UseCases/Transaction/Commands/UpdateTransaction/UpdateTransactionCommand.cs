namespace FamilyManagement.Application.UseCases.Transactions.Commands.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    string? Description,
    string? Notes,
    Guid? CategoryId,
    decimal? Amount,
    Guid? AccountId);