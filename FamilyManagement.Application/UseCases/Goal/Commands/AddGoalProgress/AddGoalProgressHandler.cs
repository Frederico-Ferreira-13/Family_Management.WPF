using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Goals.Commands.AddGoalProgress;

public sealed class AddGoalProgressHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public AddGoalProgressHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<GoalDTO>> HandleAsync(
        AddGoalProgressCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.GoalId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da meta é obrigatório.");
        }

        if (command.Amount <= 0)
        {
            return Error.Validation(
                "O valor a adicionar deve ser superior a zero.");
        }

        var goal = await _unitOfWork.Goals
            .GetGoalByIdWithDetailsAsync(command.GoalId);

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
                "Não é possível adicionar progresso a uma meta inativa.");
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            goal.TargetAmount.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var depositResult =
            goal.Deposit(moneyResult.Value);

        if (depositResult.IsFailure)
        {
            return depositResult.Error;
        }

        _unitOfWork.Goals.Update(goal);

        await _unitOfWork.CompleteAsync();

        return GoalMapper.ToDTO(goal);
    }
}