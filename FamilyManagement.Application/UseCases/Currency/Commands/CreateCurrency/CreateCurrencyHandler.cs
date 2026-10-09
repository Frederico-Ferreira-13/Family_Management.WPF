using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Commands.CreateCurrency;

public sealed class CreateCurrencyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCurrencyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CurrencyDTO>> HandleAsync(
        CreateCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = command.Code
            .Trim()
            .ToUpperInvariant();

        var exists = await _unitOfWork.Currencies.AnyAsync(c =>
            c.Code == normalizedCode);

        if (exists)
        {
            return Error.Conflict(
                "Currency.Exists",
                $"A moeda com o código {normalizedCode} já está registada.");
        }

        var currencyResult = Currency.Create(
            code: command.Code,
            name: command.Name,
            symbol: command.Symbol,
            decimalPlaces: command.DecimalPlaces,
            isDefault: command.IsDefault);

        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        var currency = currencyResult.Value;

        if (command.IsDefault)
        {
            var currentDefaults =
                await _unitOfWork.Currencies.FindAsync(c =>
                    c.IsDefault);

            foreach (var currentDefault in currentDefaults)
            {
                currentDefault.UnmarkAsDefault();
                _unitOfWork.Currencies.Update(currentDefault);
            }
        }

        await _unitOfWork.Currencies.AddAsync(currency);

        await _unitOfWork.CompleteAsync();

        return CurrencyMapper.ToDTO(currency);
    }
}