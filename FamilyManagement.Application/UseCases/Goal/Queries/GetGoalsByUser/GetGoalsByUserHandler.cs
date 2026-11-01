using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
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
        var user = await _unitOfWork.Users.GetByIdAsync(
            query.UserId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Error.Validation(
                "Não existe um utilizador ativo.");
        }

        var goals = await _unitOfWork.Goals.FindAsync(
            goal =>
                (
                    goal.WorkingCapitalKey == null &&
                    goal.UserId == query.UserId
                ) ||
                (
                    goal.WorkingCapitalKey != null &&
                    user.FamilyId.HasValue &&
                    goal.FamilyId == user.FamilyId
                ),
            cancellationToken);

        var result = goals
            .OrderByDescending(goal => goal.IsWorkingCapitalFund)
            .ThenBy(goal => goal.IsAchieved)
            .ThenBy(goal => goal.TargetDate)
            .ThenBy(goal => goal.Name)
            .Select(GoalMapper.ToDTO)
            .ToList();

        return result;
    }
}