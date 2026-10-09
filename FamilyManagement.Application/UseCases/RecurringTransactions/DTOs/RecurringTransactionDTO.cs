using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;

public sealed record RecurringTransactionDTO(
    Guid Id,
    string Description,
    decimal Amount,
    string Currency,
    TransactionType Type,
    string TypeDisplay,
    Frequency Frequency,
    string FrequencyDisplay,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime NextDueDate,
    DateTime? LastGenerateDate,
    Guid UserId,
    string? UserName,
    Guid AccountId,
    string? AccountName,
    Guid CategoryId,
    string? CategoryName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);