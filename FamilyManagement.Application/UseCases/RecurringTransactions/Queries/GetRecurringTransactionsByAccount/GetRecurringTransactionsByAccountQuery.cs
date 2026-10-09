namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries
    .GetRecurringTransactionsByAccount;

public sealed record GetRecurringTransactionsByAccountQuery(
    Guid AccountId);