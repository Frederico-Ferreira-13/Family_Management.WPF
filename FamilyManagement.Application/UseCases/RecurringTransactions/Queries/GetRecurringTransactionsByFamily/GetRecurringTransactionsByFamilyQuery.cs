namespace FamilyManagement.Application.UseCases.RecurringTransactions
    .Queries.GetRecurringTransactionsByFamily;

public sealed record GetRecurringTransactionsByFamilyQuery(
    Guid FamilyId,
    bool OnlyActive = true);