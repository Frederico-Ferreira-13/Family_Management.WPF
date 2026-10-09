using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Commands.DeactivateBudget;

public sealed class DeactivateBudgetHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateBudgetHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeactivateBudgetCommand command,
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

        if (!budget.IsActive)
        {
            return Result.Success();
        }

        budget.Deactivate();

        _unitOfWork.Budgets.Update(budget);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}