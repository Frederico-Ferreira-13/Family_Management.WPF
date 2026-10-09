using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Queries.GetGoalById;

public sealed class GetGoalByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGoalByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GoalDTO>> HandleAsync(
        GetGoalByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var goal = await _unitOfWork.Goals
            .GetGoalByIdWithDetailsAsync(query.GoalId);

        if (goal is null)
        {
            return Error.NotFound(
                "Goal.NotFound",
                "Meta não encontrada.");
        }

        return GoalMapper.ToDTO(goal);
    }
}