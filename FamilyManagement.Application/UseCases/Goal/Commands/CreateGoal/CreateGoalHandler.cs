using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Goals.Commands.CreateGoal;

public sealed class CreateGoalHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateGoalHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<GoalDTO>> HandleAsync(
        CreateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
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

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            return Error.Validation(
                "A moeda é obrigatória.");
        }

        if (command.TargetDate.HasValue &&
            command.TargetDate.Value.Date < DateTime.Today)
        {
            return Error.Validation(
                "A data alvo não pode estar no passado.");
        }

        var name = command.Name.Trim();
        var currency = command.Currency.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.IsActive)
        {
            return Error.BusinessRule(
                "User.Inactive",
                "Um utilizador inativo não pode criar metas.");
        }

        var exists = await _unitOfWork.Goals
            .GoalExistsForUserByNameAsync(
                name,
                command.UserId);

        if (exists)
        {
            return Error.Conflict(
                "Goal.Duplicate",
                $"Já existe uma meta ativa com o nome '{name}'.");
        }

        var moneyResult = Money.TryCreate(
            command.TargetAmount,
            currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var goalResult = Goal.Create(
            name,
            moneyResult.Value,
            command.UserId,
            command.TargetDate);

        if (goalResult.IsFailure)
        {
            return goalResult.Error;
        }

        var goal = goalResult.Value;

        await _unitOfWork.Goals.AddAsync(goal);

        await _unitOfWork.CompleteAsync();

        return GoalMapper.ToDTO(goal);
    }
}