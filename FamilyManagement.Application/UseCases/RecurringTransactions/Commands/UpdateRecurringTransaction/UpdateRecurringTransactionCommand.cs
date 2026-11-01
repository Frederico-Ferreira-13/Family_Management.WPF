using System;
using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.UpdateRecurringTransaction;

public sealed record UpdateRecurringTransactionCommand(
    Guid RecurringTransactionId,
    string Description,
    decimal Amount,
    TransactionType Type,
    Frequency Frequency,
    Guid CategoryId,
    Guid? AccountId,
    DateTime? EndDate,
    Guid? BeneficiaryUserId = null,
    Guid? FamilyId = null,
    Guid? FinancialPlanId = null,
    RecurringTransactionScope Scope = RecurringTransactionScope.Personal,
    DateTime? ScheduleDate = null
);