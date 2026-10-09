using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Queries.GetInvestmentById;

public sealed class GetInvestmentByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvestmentByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvestmentDTO>> HandleAsync(
        GetInvestmentByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var investment = await _unitOfWork.Investments
            .GetInvestmentByIdWithDetailsAsync(
                query.InvestmentId);

        if (investment is null)
        {
            return Error.NotFound(
                "Investment.NotFound",
                "Investimento não encontrado.");
        }

        return InvestmentMapper.ToDTO(investment);
    }
}