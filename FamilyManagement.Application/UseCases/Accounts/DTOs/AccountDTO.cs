using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Accounts.DTOs;

public sealed record AccountDTO(
    Guid Id,
    string Name,
    string? Description,
    AccountType Type,
    decimal Balance,
    string Currency,
    Guid UserId,
    Guid? FamilyId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);