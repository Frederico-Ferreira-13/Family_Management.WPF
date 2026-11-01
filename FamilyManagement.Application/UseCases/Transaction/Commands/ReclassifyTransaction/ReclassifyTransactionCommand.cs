using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Transactions.Commands
    .ReclassifyTransaction;

public sealed record ReclassifyTransactionCommand(
    Guid TransactionId,
    Guid UserId,
    RecurringTransactionScope TargetScope,
    Guid? FamilyId = null,
    Guid? FinancialPlanId = null,
    Guid? BeneficiaryUserId = null
);