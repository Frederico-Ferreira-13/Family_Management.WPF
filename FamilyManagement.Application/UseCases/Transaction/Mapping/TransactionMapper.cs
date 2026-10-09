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
            transaction.Id,
            transaction.Date,
            transaction.Amount.Amount,
            transaction.Amount.Currency,
            transaction.Description,
            transaction.Notes,
            transaction.Type,
            transaction.Type.GetDisplayName(),
            transaction.IsConfirmed,
            transaction.IsActive,
            transaction.IsRecurringGenerated,
            transaction.UserId,
            transaction.User?.UserName,
            transaction.AccountId,
            transaction.Account?.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.TargetAccountId,
            transaction.TargetAccount?.Name,
            transaction.RecurringTransactionId,
            transaction.BudgetId,
            transaction.GoalId,
            transaction.CreatedAt,
            transaction.UpdatedAt);
    }

    public static IEnumerable<TransactionDTO> ToDTOs(
        IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        return transactions.Select(ToDTO);
    }
}