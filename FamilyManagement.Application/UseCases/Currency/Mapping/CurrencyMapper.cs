using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Currencies.Mapping;

public static class CurrencyMapper
{
    public static CurrencyDTO ToDTO(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return new CurrencyDTO(
            Id: currency.Id,
            Code: currency.Code,
            Name: currency.Name,
            Symbol: currency.Symbol,
            DecimalPlaces: currency.DecimalPlaces,
            IsDefault: currency.IsDefault,
            IsActive: currency.IsActive,
            CreatedAt: currency.CreatedAt,
            UpdatedAt: currency.UpdatedAt);
    }
}