using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Goals.Commands.UpdateGoal;

public sealed class UpdateGoalHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<GoalDTO>> HandleAsync(
        UpdateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.GoalId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da meta é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.Validation(
                "O nome da meta é obrigatório.");
        }

        if (command.TargetAmount <= 0)
        {
            return Error.Validation(
                "O valor alvo deve ser superior a zero.");
        }

        if (command.TargetDate.HasValue &&
            command.TargetDate.Value.Date < DateTime.Today)
        {
            return Error.Validation(
                "A data alvo não pode estar no passado.");
        }

        var name = command.Name.Trim();

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
            return Error.BusinessRule(
                "Goal.Inactive",
                "Não é possível alterar uma meta inativa.");
        }

        if (!goal.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _unitOfWork.Goals
                .GoalExistsForUserByNameAndIdAsync(
                    name,
                    goal.UserId,
                    goal.Id);

            if (exists)
            {
                return Error.Conflict(
                    "Goal.Duplicate",
                    "Já existe outra meta com este nome.");
            }
        }

        var moneyResult = Money.TryCreate(
            command.TargetAmount,
            goal.TargetAmount.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var updateResult = goal.UpdateDetails(
            name,
            moneyResult.Value,
            command.TargetDate);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _unitOfWork.Goals.Update(goal);

        await _unitOfWork.CompleteAsync();

        return GoalMapper.ToDTO(goal);
    }
}