using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Queries.IsGoalAchieved;

public sealed class IsGoalAchievedHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public IsGoalAchievedHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> HandleAsync(
        IsGoalAchievedQuery query,
        CancellationToken cancellationToken = default)
    {
        var goal = await _unitOfWork.Goals
            .GetByIdAsync(query.GoalId);

        if (goal is null)
        {
            return Error.NotFound(
                "Goal.NotFound",
                "Meta não encontrada.");
        }

        return goal.IsAchieved;
    }
}