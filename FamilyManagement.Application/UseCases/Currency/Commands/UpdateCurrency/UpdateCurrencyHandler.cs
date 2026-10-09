using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Commands.UpdateCurrency;

public sealed class UpdateCurrencyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCurrencyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CurrencyDTO>> HandleAsync(
        UpdateCurrencyCommand command,
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

        var updateResult = currency.UpdateDetails(
            command.Name,
            command.Symbol,
            command.DecimalPlaces);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _unitOfWork.Currencies.Update(currency);

        await _unitOfWork.CompleteAsync();

        return CurrencyMapper.ToDTO(currency);
    }
}