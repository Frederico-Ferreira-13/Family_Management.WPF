using System;
using System.Collections.Generic;
using System.Linq;
using FamilyManagement.Application.Common.Extensions;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Transactions.Mapping;

public static class TransactionMapper
{
    public static TransactionDTO ToDTO(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return new TransactionDTO(
            Id: transaction.Id,
            Date: transaction.Date,
            Amount: transaction.Amount.Amount,
            Currency: transaction.Amount.Currency,
            Description: transaction.Description,
            Notes: transaction.Notes,
            Type: transaction.Type,
            TypeDisplay: transaction.Type.GetDisplayName(),
            IsConfirmed: transaction.IsConfirmed,
            IsActive: transaction.IsActive,
            IsRecurringGenerated: transaction.IsRecurringGenerated,
            UserId: transaction.UserId,
            UserName: transaction.User?.UserName,
            AccountId: transaction.AccountId,
            AccountName: transaction.Account?.Name,
            CategoryId: transaction.CategoryId,
            CategoryName: transaction.Category?.Name,
            TargetAccountId: transaction.TargetAccountId,
            TargetAccountName: transaction.TargetAccount?.Name,
            RecurringTransactionId: transaction.RecurringTransactionId,
            BudgetId: transaction.BudgetId,
            GoalId: transaction.GoalId,
            CreatedAt: transaction.CreatedAt,
            UpdatedAt: transaction.UpdatedAt,
            FamilyId: transaction.FamilyId,
            Scope: transaction.Scope,
            FinancialPlanId: transaction.FinancialPlanId,
            BeneficiaryUserId: transaction.BeneficiaryUserId,
            ScheduledDate: transaction.ScheduledDate
        );
    }

    public static IEnumerable<TransactionDTO> ToDTOs(
        IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        return transactions.Select(ToDTO);
    }
}