using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetActiveInvestmentsByUser;

public sealed class GetActiveInvestmentsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveInvestmentsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<InvestmentDTO>>> HandleAsync(
        GetActiveInvestmentsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var investments = await _unitOfWork.Investments
            .GetActiveInvestmentsByUserIdWithDetailsAsync(
                query.UserId);

        return investments
            .OrderBy(i => i.Name)
            .Select(InvestmentMapper.ToDTO)
            .ToList();
    }
}