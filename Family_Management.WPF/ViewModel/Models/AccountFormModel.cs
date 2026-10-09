using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Models;

public sealed class AccountFormModel
{
    public Guid? AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AccountType Type { get; set; } = AccountType.Checking;

    public decimal InitialBalance { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsEditMode =>
        AccountId.HasValue &&
        AccountId.Value != Guid.Empty;

    public static AccountFormModel CreateNew(
        string currency)
    {
        return new AccountFormModel
        {
            AccountId = null,
            Name = string.Empty,
            Description = string.Empty,
            Type = AccountType.Checking,
            InitialBalance = 0m,
            Currency = currency
        };
    }

    public static AccountFormModel FromAccount(
        AccountDTO account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new AccountFormModel
        {
            AccountId = account.Id,
            Name = account.Name,
            Description = account.Description,
            Type = account.Type,
            InitialBalance = account.Balance,
            Currency = account.Currency
        };
    }
}