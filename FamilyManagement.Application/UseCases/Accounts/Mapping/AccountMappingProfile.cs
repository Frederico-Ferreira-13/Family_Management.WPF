using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Accounts.Mapping;

public static class AccountMapper
{
    public static AccountDTO ToDTO(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new AccountDTO(
            account.Id,
            account.Name,
            account.Description,
            account.Type,
            account.Balance.Amount,
            account.Balance.Currency,
            account.UserId,
            account.FamilyId,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt);
    }
}