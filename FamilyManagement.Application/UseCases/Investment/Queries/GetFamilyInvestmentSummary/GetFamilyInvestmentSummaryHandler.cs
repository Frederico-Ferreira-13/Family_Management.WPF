using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetFamilyInvestmentSummary;

public sealed class GetFamilyInvestmentSummaryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFamilyInvestmentSummaryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<FamilyInvestmentSummaryDTO>>> HandleAsync(
        GetFamilyInvestmentSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var investments = await _unitOfWork.Investments
            .FindInvestmentsWithDetailsAsync(i =>
                i.IsActive &&
                i.User != null &&
                i.User.FamilyId == query.FamilyId);

        var result = investments
            .GroupBy(i => i.CurrentValue.Currency)
            .Select(group => new FamilyInvestmentSummaryDTO(
                Currency: group.Key,
                TotalInitialValue: group.Sum(i =>
                    i.InitialValue.Amount),
                TotalCurrentValue: group.Sum(i =>
                    i.CurrentValue.Amount),
                TotalProfitLoss: group.Sum(i =>
                    i.ProfitLoss),
                InvestmentCount: group.Count()))
            .OrderBy(item => item.Currency)
            .ToList();

        return result;
    }
}