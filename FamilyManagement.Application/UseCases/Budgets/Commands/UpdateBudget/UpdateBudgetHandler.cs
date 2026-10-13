using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Budgets.Commands.UpdateBudget;

public sealed class UpdateBudgetHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBudgetHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        UpdateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.BudgetId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do orçamento é obrigatório.");
        }

        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        // GetByIdAsync permite obter uma entidade acompanhada
        // pelo EF Core para posterior alteração.
        var budget = await _unitOfWork.Budgets
            .GetByIdAsync(command.BudgetId);

        if (budget is null)
        {
            return Error.NotFound(
                "Budget.NotFound",
                "Orçamento não encontrado.");
        }

        if (budget.FamilyId.HasValue)
        {
            // Qualquer membro ativo da mesma família
            // pode editar um orçamento familiar.
            if (user.FamilyId != budget.FamilyId)
            {
                return Error.BusinessRule(
                    "Budget.AccessDenied",
                    "Não tem autorização para editar este orçamento.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(budget.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.BusinessRule(
                    "Budget.FamilyUnavailable",
                    "A família não existe ou está inativa.");
            }
        }
        else
        {
            // Um orçamento pessoal só pode ser editado
            // pelo respetivo proprietário.
            if (budget.UserId != command.UserId)
            {
                return Error.BusinessRule(
                    "Budget.AccessDenied",
                    "Não tem autorização para editar este orçamento.");
            }
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            budget.BudgetedAmount.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var updateResult = budget.UpdateDetails(
            command.Name,
            moneyResult.Value);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _unitOfWork.Budgets.Update(budget);

        await _unitOfWork.CompleteAsync();

        return Result<BudgetDTO>.Success(
            BudgetMapper.ToDTO(budget));
    }
}