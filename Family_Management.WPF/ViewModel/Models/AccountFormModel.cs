using System.ComponentModel;
using System.Runtime.CompilerServices;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Models;

public sealed class AccountFormModel : INotifyPropertyChanged
{
    private Guid? _accountId;
    private string _name = string.Empty;
    private string _bankName = string.Empty;
    private string? _description;
    private AccountType _type = AccountType.Checking;
    private decimal _initialBalance;
    private string _currency = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? AccountId
    {
        get => _accountId;
        set
        {
            if (SetProperty(ref _accountId, value))
                OnPropertyChanged(nameof(IsEditMode));
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string BankName
    {
        get => _bankName;
        set => SetProperty(ref _bankName, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public AccountType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public decimal InitialBalance
    {
        get => _initialBalance;
        set => SetProperty(ref _initialBalance, value);
    }

    public string Currency
    {
        get => _currency;
        set => SetProperty(ref _currency, value);
    }

    public bool IsEditMode =>
        AccountId.HasValue && AccountId.Value != Guid.Empty;

    public static AccountFormModel CreateNew(string currency)
    {
        return new AccountFormModel
        {
            AccountId = null,
            Name = string.Empty,
            BankName = string.Empty,
            Description = null,
            Type = AccountType.Checking,
            InitialBalance = 0m,
            Currency = currency
        };
    }

    public static AccountFormModel FromAccount(AccountDTO account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new AccountFormModel
        {
            AccountId = account.Id,
            Name = account.Name,
            BankName = account.BankName,
            Description = account.Description,
            Type = account.Type,
            InitialBalance = account.Balance,
            Currency = account.Currency
        };
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}