using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Commands.ActivateCurrency;

public sealed class ActivateCurrencyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivateCurrencyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        ActivateCurrencyCommand command,
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

        currency.Activate();

        _unitOfWork.Currencies.Update(currency);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}