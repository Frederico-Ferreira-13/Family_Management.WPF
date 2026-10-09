using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetMonthlyBudgetSummary;

public sealed class GetMonthlyBudgetSummaryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMonthlyBudgetSummaryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MonthlyBudgetSummaryDTO>> HandleAsync(
        GetMonthlyBudgetSummaryQuery query,
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

        var (totalBudgeted, totalSpent) =
            await _unitOfWork.Budgets.GetMonthlySummaryAsync(
                query.UserId,
                query.Month,
                query.Year);

        var remainingAmount =
            totalBudgeted - totalSpent;

        return new MonthlyBudgetSummaryDTO(
            UserId: query.UserId,
            Month: query.Month,
            Year: query.Year,
            TotalBudgeted: totalBudgeted,
            TotalSpent: totalSpent,
            RemainingAmount: remainingAmount,
            IsOverBudget: totalSpent > totalBudgeted);
    }
}