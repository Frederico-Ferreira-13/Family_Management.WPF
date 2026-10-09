using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransaction;

public sealed record CreateTransactionCommand(
    DateTime Date,
    decimal Amount,
    string Currency,
    string Description,
    string? Notes,
    TransactionType Type,
    Guid AccountId,
    Guid UserId,
    Guid CategoryId,
    Guid? BudgetId = null,
    Guid? GoalId = null,
    bool IsConfirmed = true);