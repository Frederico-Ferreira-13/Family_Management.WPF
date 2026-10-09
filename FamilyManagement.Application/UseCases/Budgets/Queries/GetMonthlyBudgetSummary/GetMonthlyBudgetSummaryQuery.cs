namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetMonthlyBudgetSummary;

public sealed record GetMonthlyBudgetSummaryQuery(
    Guid UserId,
    int Month,
    int Year);