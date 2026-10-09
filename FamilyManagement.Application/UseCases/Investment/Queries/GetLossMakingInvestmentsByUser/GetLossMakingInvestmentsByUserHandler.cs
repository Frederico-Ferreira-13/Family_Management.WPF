using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetLossMakingInvestmentsByUser;

public sealed class GetLossMakingInvestmentsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLossMakingInvestmentsByUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<InvestmentDTO>>> HandleAsync(
        GetLossMakingInvestmentsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var investments = await _unitOfWork.Investments
            .GetLossMakingInvestmentsByUserIdAsync(query.UserId);

        return investments
            .OrderBy(i => i.ProfitLossPercentage)
            .Select(InvestmentMapper.ToDTO)
            .ToList();
    }
}