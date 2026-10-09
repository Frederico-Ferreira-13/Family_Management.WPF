namespace FamilyManagement.Application.UseCases.Investments.Commands.DeactivateInvestment;

public sealed record DeactivateInvestmentCommand(
    Guid InvestmentId);