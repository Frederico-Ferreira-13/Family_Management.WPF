using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.UpdateAccount;

public sealed record UpdateAccountCommand(
    Guid AccountId,
    string Name,
    string? Description,
    AccountType Type
);