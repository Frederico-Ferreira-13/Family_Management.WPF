namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetsByUserAndPeriod;

public sealed record GetBudgetsByUserAndPeriodQuery(
    Guid UserId,
    int Month,
    int Year);