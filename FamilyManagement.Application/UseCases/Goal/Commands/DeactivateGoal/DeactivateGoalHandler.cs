using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Commands.DeactivateGoal;

public sealed class DeactivateGoalHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateGoalHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        DeactivateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.GoalId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da meta é obrigatório.");
        }

        var goal = await _unitOfWork.Goals
            .GetByIdAsync(command.GoalId);

        if (goal is null)
        {
            return Error.NotFound(
                "Goal.NotFound",
                "Meta não encontrada.");
        }

        if (!goal.IsActive)
        {
            return Result.Success();
        }

        goal.Deactivate();

        _unitOfWork.Goals.Update(goal);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}