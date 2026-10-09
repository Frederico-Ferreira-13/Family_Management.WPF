using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string Name,
    string? Description,
    AccountType Type,
    decimal InitialBalance,
    string Currency,
    Guid UserId,
    Guid? FamilyId
);