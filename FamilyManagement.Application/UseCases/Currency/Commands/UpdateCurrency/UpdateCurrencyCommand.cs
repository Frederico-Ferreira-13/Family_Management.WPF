namespace FamilyManagement.Application.UseCases.Currencies.Commands.UpdateCurrency;

public sealed record UpdateCurrencyCommand(
    Guid CurrencyId,
    string Name,
    string Symbol,
    byte DecimalPlaces);