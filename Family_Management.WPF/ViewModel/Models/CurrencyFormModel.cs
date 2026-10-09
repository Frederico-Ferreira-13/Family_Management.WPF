using System.ComponentModel;
using System.Runtime.CompilerServices;
using FamilyManagement.Application.UseCases.Currencies.DTOs;

namespace Family_Management.WPF.ViewModel.Models;

public sealed class CurrencyFormModel : INotifyPropertyChanged
{
    private Guid? _currencyId;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _symbol = string.Empty;
    private byte _decimalPlaces = 2;
    private bool _isDefault;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? CurrencyId
    {
        get => _currencyId;

        set
        {
            if (SetProperty(ref _currencyId, value))
            {
                OnPropertyChanged(nameof(IsEditMode));
            }
        }
    }

    public string Code
    {
        get => _code;
        set => SetProperty(
            ref _code,
            value ?? string.Empty);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(
            ref _name,
            value ?? string.Empty);
    }

    public string Symbol
    {
        get => _symbol;
        set => SetProperty(
            ref _symbol,
            value ?? string.Empty);
    }

    public byte DecimalPlaces
    {
        get => _decimalPlaces;
        set => SetProperty(
            ref _decimalPlaces,
            value);
    }

    public bool IsDefault
    {
        get => _isDefault;
        set => SetProperty(
            ref _isDefault,
            value);
    }

    public bool IsEditMode =>
        CurrencyId.HasValue &&
        CurrencyId.Value != Guid.Empty;

    public static CurrencyFormModel CreateNew()
    {
        return new CurrencyFormModel
        {
            CurrencyId = null,
            Code = string.Empty,
            Name = string.Empty,
            Symbol = string.Empty,
            DecimalPlaces = 2,
            IsDefault = false
        };
    }

    public static CurrencyFormModel FromCurrency(
        CurrencyDTO currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return new CurrencyFormModel
        {
            CurrencyId = currency.Id,
            Code = currency.Code,
            Name = currency.Name,
            Symbol = currency.Symbol,
            DecimalPlaces = currency.DecimalPlaces,
            IsDefault = currency.IsDefault
        };
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(
                field,
                value))
        {
            return false;
        }

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