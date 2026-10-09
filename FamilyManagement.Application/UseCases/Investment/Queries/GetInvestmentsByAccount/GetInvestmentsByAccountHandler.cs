using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetInvestmentsByAccount;

public sealed class GetInvestmentsByAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvestmentsByAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<InvestmentDTO>>> HandleAsync(
        GetInvestmentsByAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        var investments = await _unitOfWork.Investments
            .GetInvestmentsByAccountIdWithDetailsAsync(
                query.AccountId);

        return investments
            .OrderBy(i => i.Name)
            .Select(InvestmentMapper.ToDTO)
            .ToList();
    }
}