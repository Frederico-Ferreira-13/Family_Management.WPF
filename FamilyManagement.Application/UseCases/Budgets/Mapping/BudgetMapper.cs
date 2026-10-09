using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Budgets.Mapping;

public static class BudgetMapper
{
    public static BudgetDTO ToDTO(Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        return new BudgetDTO(
            Id: budget.Id,
            Name: budget.Name,
            BudgetedAmount: budget.BudgetedAmount.Amount,
            CurrentSpent: budget.CurrentSpent.Amount,
            RemainingAmount: budget.RemainingAmount,
            Currency: budget.BudgetedAmount.Currency,
            Month: budget.Month,
            Year: budget.Year,
            CategoryId: budget.CategoryId,
            UserId: budget.UserId,
            IsOverBudget: budget.IsOverBudget,
            IsActive: budget.IsActive,
            CreatedAt: budget.CreatedAt,
            UpdatedAt: budget.UpdatedAt);
    }
}