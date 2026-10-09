namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands
    .GeneratePendingRecurringTransactions;

public sealed record GeneratePendingRecurringTransactionsCommand(
    Guid UserId,
    DateTime? AsOfDate = null);