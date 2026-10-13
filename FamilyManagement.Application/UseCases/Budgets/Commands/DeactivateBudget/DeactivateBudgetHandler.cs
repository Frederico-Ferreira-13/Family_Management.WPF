using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Commands.DeactivateBudget;

public sealed class DeactivateBudgetHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateBudgetHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        DeactivateBudgetCommand command,
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
            if (user.FamilyId != budget.FamilyId)
            {
                return Error.BusinessRule(
                    "Budget.AccessDenied",
                    "Não tem autorização para desativar este orçamento.");
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
            if (budget.UserId != command.UserId)
            {
                return Error.BusinessRule(
                    "Budget.AccessDenied",
                    "Não tem autorização para desativar este orçamento.");
            }
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