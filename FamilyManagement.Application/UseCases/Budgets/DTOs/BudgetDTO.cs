namespace FamilyManagement.Application.UseCases.Budgets.DTOs;

public sealed record BudgetDTO(
    Guid Id,
    string Name,
    decimal BudgetedAmount,
    decimal CurrentSpent,
    decimal RemainingAmount,
    string Currency,
    int Month,
    int Year,
    Guid CategoryId,
    Guid UserId,
    bool IsOverBudget,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);