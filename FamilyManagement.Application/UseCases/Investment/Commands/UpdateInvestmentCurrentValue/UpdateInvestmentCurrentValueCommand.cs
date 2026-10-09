namespace FamilyManagement.Application.UseCases.Investments.Commands.UpdateInvestmentCurrentValue;

public sealed record UpdateInvestmentCurrentValueCommand(
    Guid InvestmentId,
    decimal CurrentValue);