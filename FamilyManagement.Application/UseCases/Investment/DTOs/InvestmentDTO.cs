using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Investments.DTOs;

public sealed record InvestmentDTO(
    Guid Id,
    string Name,
    InvestmentType Type,
    string TypeDisplay,
    decimal InitialValue,
    decimal CurrentValue,
    string Currency,
    decimal ProfitLoss,
    double ProfitLossPercentage,
    bool IsProfitable,
    DateTime PurchaseDate,
    DateTime? LastUpdateDate,
    Guid UserId,
    string? UserName,
    Guid? AccountId,
    string? AccountName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);