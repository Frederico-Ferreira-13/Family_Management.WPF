using System;

namespace FamilyManagement.Application.UseCases.FinancialPlans;

public sealed record FinancialPlanSummaryDTO(
    Guid FinancialPlanId,
    string Name,
    string CurrencyCode,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance);
