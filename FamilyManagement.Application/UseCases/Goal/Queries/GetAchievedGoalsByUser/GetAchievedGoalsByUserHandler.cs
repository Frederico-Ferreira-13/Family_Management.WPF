using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Queries.GetAchievedGoalsByUser;

public sealed class GetAchievedGoalsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAchievedGoalsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<GoalDTO>>> HandleAsync(
        GetAchievedGoalsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var goals = await _unitOfWork.Goals
            .GetAchievedGoalsByUserIdAsync(query.UserId);

        var result = goals
            .Select(GoalMapper.ToDTO)
            .ToList();

        return result;
    }
}