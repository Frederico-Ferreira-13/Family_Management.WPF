using System;

namespace FamilyManagement.Application.UseCases.FinancialPlans;

public sealed record CreateFamilyFinancialPlanCommand(
    Guid FamilyId,
    Guid CreatedByUserId,
    string Name,
    string CurrencyCode,
    string? Description = null);
