using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Queries.GetGoalsByUser;

public sealed class GetGoalsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGoalsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<GoalDTO>>> HandleAsync(
        GetGoalsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var goals = await _unitOfWork.Goals
            .GetGoalsByUserIdWithDetailsAsync(query.UserId);

        var result = goals
            .OrderBy(goal => goal.IsAchieved)
            .ThenBy(goal => goal.TargetDate)
            .Select(GoalMapper.ToDTO)
            .ToList();

        return result;
    }
}