namespace FamilyManagement.Application.UseCases.Budgets.DTOs;

public sealed record MonthlyBudgetSummaryDTO(
    Guid UserId,
    int Month,
    int Year,
    decimal TotalBudgeted,
    decimal TotalSpent,
    decimal RemainingAmount,
    bool IsOverBudget);