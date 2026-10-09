namespace FamilyManagement.Application.UseCases.Goals.Commands.CreateGoal;

public sealed record CreateGoalCommand(
    string Name,
    decimal TargetAmount,
    string Currency,
    Guid UserId,
    DateTime? TargetDate);