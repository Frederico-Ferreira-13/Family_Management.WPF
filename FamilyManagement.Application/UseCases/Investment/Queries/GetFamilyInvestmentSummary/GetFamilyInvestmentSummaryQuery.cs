namespace FamilyManagement.Application.UseCases.Investments.DTOs;

public sealed record FamilyInvestmentSummaryDTO(
    string Currency,
    decimal TotalInitialValue,
    decimal TotalCurrentValue,
    decimal TotalProfitLoss,
    int InvestmentCount);