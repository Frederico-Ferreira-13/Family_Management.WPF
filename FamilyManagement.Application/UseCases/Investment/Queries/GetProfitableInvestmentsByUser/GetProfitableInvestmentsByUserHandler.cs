using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetProfitableInvestmentsByUser;

public sealed class GetProfitableInvestmentsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProfitableInvestmentsByUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<InvestmentDTO>>> HandleAsync(
        GetProfitableInvestmentsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var investments = await _unitOfWork.Investments
            .GetProfitableInvestmentsByUserIdAsync(query.UserId);

        return investments
            .OrderByDescending(i => i.ProfitLossPercentage)
            .Select(InvestmentMapper.ToDTO)
            .ToList();
    }
}