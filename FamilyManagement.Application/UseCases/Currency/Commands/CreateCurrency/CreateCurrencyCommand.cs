namespace FamilyManagement.Application.UseCases.Currencies.Commands.CreateCurrency;

public sealed record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    byte DecimalPlaces = 2,
    bool IsDefault = false);