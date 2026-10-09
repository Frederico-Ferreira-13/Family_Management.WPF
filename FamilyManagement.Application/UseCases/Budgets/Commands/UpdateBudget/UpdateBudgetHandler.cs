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
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        UpdateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        var budget = await _unitOfWork.Budgets
            .GetByIdAsync(command.BudgetId);

        if (budget is null)
        {
            return Error.NotFound(
                "Budget.NotFound",
                "Orçamento não encontrado.");
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

        return BudgetMapper.ToDTO(budget);
    }
}