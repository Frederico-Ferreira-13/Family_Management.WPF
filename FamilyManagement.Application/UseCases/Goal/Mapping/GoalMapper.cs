using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Goals.Mapping;

public static class GoalMapper
{
    public static GoalDTO ToDTO(Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);

        return new GoalDTO(
            Id: goal.Id,
            Name: goal.Name,
            TargetAmount: goal.TargetAmount.Amount,
            CurrentAmount: goal.CurrentAmount.Amount,
            RemainingAmount: goal.RemainingAmount.Amount,
            Currency: goal.TargetAmount.Currency,
            ProgressPercentage: goal.ProgressPercentage,
            StartDate: goal.StartDate,
            TargetDate: goal.TargetDate,
            IsAchieved: goal.IsAchieved,
            UserId: goal.UserId,
            UserName: goal.User?.UserName,
            IsActive: goal.IsActive,
            CreatedAt: goal.CreatedAt,
            UpdatedAt: goal.UpdatedAt);
    }
}