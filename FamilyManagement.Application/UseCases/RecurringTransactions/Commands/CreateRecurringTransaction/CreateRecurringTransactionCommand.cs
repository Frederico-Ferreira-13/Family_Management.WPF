using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;

public sealed record CreateRecurringTransactionCommand(
    string Description,
    decimal Amount,
    string Currency,
    TransactionType Type,
    Frequency Frequency,
    DateTime StartDate,
    DateTime? EndDate,
    Guid CategoryId,
    Guid AccountId,
    Guid UserId);