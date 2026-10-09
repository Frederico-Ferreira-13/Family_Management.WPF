namespace FamilyManagement.Application.UseCases.RecurringTransactions
    .Queries.GetUpcomingRecurringTransactions;

public sealed record GetUpcomingRecurringTransactionsQuery(
    Guid FamilyId,
    int DaysLookahead = 7);