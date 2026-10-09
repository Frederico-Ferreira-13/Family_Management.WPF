using FamilyManagement.Application.Common.Extensions;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;

public static class RecurringTransactionMapper
{
    public static RecurringTransactionDTO ToDTO(
        RecurringTransaction recurringTransaction)
    {
        ArgumentNullException.ThrowIfNull(recurringTransaction);

        return new RecurringTransactionDTO(
            Id: recurringTransaction.Id,
            Description: recurringTransaction.Description,
            Amount: recurringTransaction.Amount.Amount,
            Currency: recurringTransaction.Amount.Currency,
            Type: recurringTransaction.Type,
            TypeDisplay: recurringTransaction.Type.GetDisplayName(),
            Frequency: recurringTransaction.Frequency,
            FrequencyDisplay: recurringTransaction.Frequency.GetDisplayName(),
            StartDate: recurringTransaction.StartDate,
            EndDate: recurringTransaction.EndDate,
            NextDueDate: recurringTransaction.NextDueDate,
            LastGenerateDate: recurringTransaction.LastGenerateDate,
            UserId: recurringTransaction.UserId,
            UserName: recurringTransaction.User?.UserName,
            AccountId: recurringTransaction.AccountId,
            AccountName: recurringTransaction.Account?.Name,
            CategoryId: recurringTransaction.CategoryId,
            CategoryName: recurringTransaction.Category?.Name,
            IsActive: recurringTransaction.IsActive,
            CreatedAt: recurringTransaction.CreatedAt,
            UpdatedAt: recurringTransaction.UpdatedAt);
    }
}