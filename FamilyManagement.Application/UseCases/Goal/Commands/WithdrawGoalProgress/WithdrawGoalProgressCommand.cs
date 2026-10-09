namespace FamilyManagement.Application.UseCases.Goals.Commands.WithdrawGoalProgress;

public sealed record WithdrawGoalProgressCommand(
    Guid GoalId,
    decimal Amount);