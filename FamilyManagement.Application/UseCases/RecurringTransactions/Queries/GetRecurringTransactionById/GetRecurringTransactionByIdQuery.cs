namespace FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetRecurringTransactionById;

public sealed record GetRecurringTransactionByIdQuery(
    Guid RecurringTransactionId);