using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Investments.Commands.UpdateInvestment;

public sealed record UpdateInvestmentCommand(
    Guid InvestmentId,
    string Name,
    InvestmentType Type,
    Guid? AccountId);