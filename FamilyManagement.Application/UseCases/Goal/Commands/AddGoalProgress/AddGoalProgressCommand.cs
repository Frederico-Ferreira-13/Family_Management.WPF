namespace FamilyManagement.Application.UseCases.Goals.Commands.AddGoalProgress;

public sealed record AddGoalProgressCommand(
    Guid GoalId,
    decimal Amount);