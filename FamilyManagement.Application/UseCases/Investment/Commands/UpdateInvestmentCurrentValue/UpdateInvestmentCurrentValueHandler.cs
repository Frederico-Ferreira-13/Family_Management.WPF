using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Investments.Commands.UpdateInvestmentCurrentValue;

public sealed class UpdateInvestmentCurrentValueHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateInvestmentCurrentValueHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvestmentDTO>> HandleAsync(
        UpdateInvestmentCurrentValueCommand command,
        CancellationToken cancellationToken = default)
    {
        var investment = await _unitOfWork.Investments
            .GetByIdAsync(command.InvestmentId);

        if (investment is null)
        {
            return Error.NotFound(
                "Investment.NotFound",
                "Investimento não encontrado.");
        }

        var moneyResult = Money.TryCreate(
            command.CurrentValue,
            investment.InitialValue.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var updateResult = investment.UpdateCurrentValue(
            moneyResult.Value);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _unitOfWork.Investments.Update(investment);

        await _unitOfWork.CompleteAsync();

        return InvestmentMapper.ToDTO(investment);
    }
}