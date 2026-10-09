namespace FamilyManagement.Application.UseCases.Investments.Queries.GetProfitableInvestmentsByUser;

public sealed record GetProfitableInvestmentsByUserQuery(
    Guid UserId);