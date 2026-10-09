namespace FamilyManagement.Application.UseCases.Transactions.Commands.DeactivateTransaction;

public sealed record DeactivateTransactionCommand(
    Guid TransactionId);