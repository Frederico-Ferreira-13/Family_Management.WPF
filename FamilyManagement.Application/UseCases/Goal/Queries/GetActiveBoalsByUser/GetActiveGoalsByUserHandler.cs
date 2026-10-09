using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Queries.GetActiveGoalsByUser;

public sealed class GetActiveGoalsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveGoalsByUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<IReadOnlyCollection<GoalDTO>>> HandleAsync(
        GetActiveGoalsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var goals =
            await _unitOfWork.Goals
                .GetPendingGoalsByUserIdWithDetailsAsync(
                    query.UserId);

        cancellationToken.ThrowIfCancellationRequested();

        var result = goals
            .Where(goal => goal.IsActive)
            .OrderBy(goal => goal.TargetDate)
            .Select(GoalMapper.ToDTO)
            .ToList();

        return result;
    }
}