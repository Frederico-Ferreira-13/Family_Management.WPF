using System.ComponentModel;
using System.Runtime.CompilerServices;
using FamilyManagement.Application.UseCases.Budgets.DTOs;

namespace Family_Management.WPF.ViewModel.Models;

public sealed class BudgetFormModel : INotifyPropertyChanged
{
    private Guid? _budgetId;
    private string _name = string.Empty;
    private decimal _amount;
    private string _currency = string.Empty;
    private int _month;
    private int _year;
    private Guid? _categoryId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? BudgetId
    {
        get => _budgetId;
        set
        {
            if (SetProperty(ref _budgetId, value))
            {
                OnPropertyChanged(nameof(IsEditMode));
            }
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(
            ref _name,
            value ?? string.Empty);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public string Currency
    {
        get => _currency;
        set => SetProperty(
            ref _currency,
            value ?? string.Empty);
    }

    public int Month
    {
        get => _month;
        set => SetProperty(ref _month, value);
    }

    public int Year
    {
        get => _year;
        set => SetProperty(ref _year, value);
    }

    public Guid? CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    public bool IsEditMode =>
        BudgetId.HasValue &&
        BudgetId.Value != Guid.Empty;

    public static BudgetFormModel CreateNew(
        string currency,
        int month,
        int year)
    {
        return new BudgetFormModel
        {
            BudgetId = null,
            Name = string.Empty,
            Amount = 0m,
            Currency = currency,
            Month = month,
            Year = year,
            CategoryId = null
        };
    }

    public static BudgetFormModel FromBudget(
        BudgetDTO budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        return new BudgetFormModel
        {
            BudgetId = budget.Id,
            Name = budget.Name,
            Amount = budget.BudgetedAmount,
            Currency = budget.Currency,
            Month = budget.Month,
            Year = budget.Year,
            CategoryId = budget.CategoryId
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