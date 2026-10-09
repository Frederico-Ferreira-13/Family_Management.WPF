using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Goals.Commands.WithdrawGoalProgress;

public sealed class WithdrawGoalProgressHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public WithdrawGoalProgressHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<GoalDTO>> HandleAsync(
        WithdrawGoalProgressCommand command,
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
                "O valor a retirar deve ser superior a zero.");
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
                "Não é possível retirar progresso de uma meta inativa.");
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            goal.TargetAmount.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var withdrawResult =
            goal.Withdraw(moneyResult.Value);

        if (withdrawResult.IsFailure)
        {
            return withdrawResult.Error;
        }

        _unitOfWork.Goals.Update(goal);

        await _unitOfWork.CompleteAsync();

        return GoalMapper.ToDTO(goal);
    }
}