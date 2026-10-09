using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Commands.DeactivateCurrency;

public sealed class DeactivateCurrencyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCurrencyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeactivateCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        var currency = await _unitOfWork.Currencies
            .GetByIdAsync(command.CurrencyId);

        if (currency is null)
        {
            return Error.NotFound(
                "Currency.NotFound",
                "Moeda não encontrada.");
        }

        var result = currency.Disable();

        if (result.IsFailure)
        {
            return result.Error;
        }

        _unitOfWork.Currencies.Update(currency);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}