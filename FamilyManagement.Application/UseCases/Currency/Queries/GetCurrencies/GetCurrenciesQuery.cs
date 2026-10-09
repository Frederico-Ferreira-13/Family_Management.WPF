namespace FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;

public sealed record GetCurrenciesQuery(
    bool OnlyActive = true);