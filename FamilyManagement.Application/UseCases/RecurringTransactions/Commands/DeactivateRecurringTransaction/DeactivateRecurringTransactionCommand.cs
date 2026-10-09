namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.DeactivateRecurringTransaction;

public sealed record DeactivateRecurringTransactionCommand(
    Guid RecurringTransactionId);