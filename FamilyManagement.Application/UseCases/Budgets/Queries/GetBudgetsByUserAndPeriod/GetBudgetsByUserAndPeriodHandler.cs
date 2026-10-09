using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetsByUserAndPeriod;

public sealed class GetBudgetsByUserAndPeriodHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetsByUserAndPeriodHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<BudgetDTO>>> HandleAsync(
        GetBudgetsByUserAndPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
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

        var budgets = await _unitOfWork.Budgets
            .GetBudgetsByUserAndMonthYearAsync(
                query.UserId,
                query.Month,
                query.Year);

        var result = budgets
            .Select(BudgetMapper.ToDTO)
            .ToList();

        return result;
    }
}