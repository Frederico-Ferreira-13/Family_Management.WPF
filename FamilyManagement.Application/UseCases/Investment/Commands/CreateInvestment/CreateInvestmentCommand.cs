using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Investments.Commands.CreateInvestment;

public sealed record CreateInvestmentCommand(
    string Name,
    InvestmentType Type,
    decimal InitialValue,
    string Currency,
    DateTime PurchaseDate,
    Guid UserId,
    Guid? AccountId);