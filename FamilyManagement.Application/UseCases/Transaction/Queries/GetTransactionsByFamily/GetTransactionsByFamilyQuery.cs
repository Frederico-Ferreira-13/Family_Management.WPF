namespace FamilyManagement.Application.UseCases.Transactions.Queries
    .GetTransactionsByFamily;

public sealed record GetTransactionsByFamilyQuery(
    Guid FamilyId,
    DateTime StartDate,
    DateTime EndDate);