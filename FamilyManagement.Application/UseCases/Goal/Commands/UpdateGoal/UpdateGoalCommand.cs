namespace FamilyManagement.Application.UseCases.Goals.Commands.UpdateGoal;

public sealed record UpdateGoalCommand(
    Guid GoalId,
    string Name,
    decimal TargetAmount,
    DateTime? TargetDate);