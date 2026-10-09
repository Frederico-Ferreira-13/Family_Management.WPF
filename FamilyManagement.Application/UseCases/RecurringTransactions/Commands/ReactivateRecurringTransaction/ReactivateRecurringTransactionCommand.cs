namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.ReactivateRecurringTransaction;

public sealed record ReactivateRecurringTransactionCommand(
    Guid RecurringTransactionId);