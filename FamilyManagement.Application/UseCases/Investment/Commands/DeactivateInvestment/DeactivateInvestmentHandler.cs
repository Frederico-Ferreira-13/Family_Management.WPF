using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Commands.DeactivateInvestment;

public sealed class DeactivateInvestmentHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateInvestmentHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeactivateInvestmentCommand command,
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

        var deactivateResult =
            investment.DeactivateInvestment();

        if (deactivateResult.IsFailure)
        {
            return deactivateResult.Error;
        }

        _unitOfWork.Investments.Update(investment);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}