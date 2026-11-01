
using System;

namespace FamilyManagement.Application.UseCases.RecurringTransactions
    .Commands.AssociateRecurringTransactionWithFamily;

public sealed record AssociateRecurringTransactionWithFamilyCommand(
    Guid RecurringTransactionId,
    Guid FamilyId,
    Guid BeneficiaryUserId,
    Guid RequestedByUserId,
    Guid FinancialPlanId
);
