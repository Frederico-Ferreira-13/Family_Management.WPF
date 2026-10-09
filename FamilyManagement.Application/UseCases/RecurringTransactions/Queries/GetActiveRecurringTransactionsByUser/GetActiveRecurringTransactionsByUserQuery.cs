namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetActiveRecurringTransactionsByUser;

public sealed record GetActiveRecurringTransactionsByUserQuery(
    Guid UserId);