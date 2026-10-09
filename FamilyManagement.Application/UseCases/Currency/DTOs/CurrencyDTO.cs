namespace FamilyManagement.Application.UseCases.Currencies.DTOs;

public sealed record CurrencyDTO(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    byte DecimalPlaces,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);