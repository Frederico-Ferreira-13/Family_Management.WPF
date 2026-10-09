using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Transactions.DTOs;

public sealed record TransactionDTO(
    Guid Id,
    DateTime Date,
    decimal Amount,
    string Currency,
    string Description,
    string? Notes,
    TransactionType Type,
    string TypeDisplay,
    bool IsConfirmed,
    bool IsActive,
    bool IsRecurringGenerated,
    Guid UserId,
    string? UserName,
    Guid AccountId,
    string? AccountName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? TargetAccountId,
    string? TargetAccountName,
    Guid? RecurringTransactionId,
    Guid? BudgetId,
    Guid? GoalId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);