using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetInvestmentsByUser;

public sealed class GetInvestmentsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvestmentsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<InvestmentDTO>>> HandleAsync(
        GetInvestmentsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var investments = await _unitOfWork.Investments
            .GetInvestmentsByUserIdWithDetailsAsync(query.UserId);

        var result = investments
            .OrderBy(i => i.Name)
            .Select(InvestmentMapper.ToDTO)
            .ToList();

        return result;
    }
}