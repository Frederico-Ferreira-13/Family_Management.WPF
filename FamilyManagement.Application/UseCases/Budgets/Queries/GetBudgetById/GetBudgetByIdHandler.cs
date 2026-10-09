using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetById;

public sealed class GetBudgetByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        GetBudgetByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var budget = await _unitOfWork.Budgets
            .GetBudgetByIdWithDetailsAsync(query.BudgetId);

        if (budget is null)
        {
            return Error.NotFound(
                "Budget.NotFound",
                "Orçamento não encontrado.");
        }

        return BudgetMapper.ToDTO(budget);
    }
}