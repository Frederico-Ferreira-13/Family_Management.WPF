using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetByCategoryAndPeriod;

public sealed class GetBudgetByCategoryAndPeriodHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetByCategoryAndPeriodHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        GetBudgetByCategoryAndPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CategoryId == Guid.Empty)
        {
            return Error.Validation(
                "A categoria é obrigatória.");
        }

        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (query.Month is < 1 or > 12)
        {
            return Error.Validation(
                "O mês deve estar entre 1 e 12.");
        }

        if (query.Year is < 2000 or > 9999)
        {
            return Error.Validation(
                "O ano é inválido.");
        }

        var budget = await _unitOfWork.Budgets
            .GetBudgetsByCategoryIdAndUserIdAndMonthYearAsync(
                query.CategoryId,
                query.UserId,
                query.Month,
                query.Year);

        if (budget is null)
        {
            return Error.NotFound(
                "Budget.NotFound",
                "Orçamento não encontrado.");
        }

        return BudgetMapper.ToDTO(budget);
    }
}