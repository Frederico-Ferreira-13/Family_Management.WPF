namespace FamilyManagement.Application.UseCases.Goals.DTOs;

public sealed record GoalDTO(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    string Currency,
    double ProgressPercentage,
    DateTime StartDate,
    DateTime? TargetDate,
    bool IsAchieved,
    Guid UserId,
    string? UserName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);