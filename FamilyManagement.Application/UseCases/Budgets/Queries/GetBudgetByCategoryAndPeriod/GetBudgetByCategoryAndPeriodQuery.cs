namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetByCategoryAndPeriod;

public sealed record GetBudgetByCategoryAndPeriodQuery(
    Guid CategoryId,
    Guid UserId,
    int Month,
    int Year);