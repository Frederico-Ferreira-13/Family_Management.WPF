using System.ComponentModel;
using System.Runtime.CompilerServices;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Models;

public sealed class CategoryFormModel : INotifyPropertyChanged
{
    private Guid? _categoryId;
    private string _name = string.Empty;
    private string? _description;
    private CategoryType _type = CategoryType.Expense;
    private CategoryScope _scope = CategoryScope.Personal;
    private Guid? _parentCategoryId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? CategoryId
    {
        get => _categoryId;
        set
        {
            if (SetProperty(ref _categoryId, value))
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

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public CategoryType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public CategoryScope Scope
    {
        get => _scope;
        set => SetProperty(ref _scope, value);
    }

    public Guid? ParentCategoryId
    {
        get => _parentCategoryId;
        set => SetProperty(ref _parentCategoryId, value);
    }

    public bool IsEditMode =>
        CategoryId.HasValue &&
        CategoryId.Value != Guid.Empty;

    public static CategoryFormModel CreateNew()
    {
        return new CategoryFormModel
        {
            CategoryId = null,
            Name = string.Empty,
            Description = string.Empty,
            Type = CategoryType.Expense,
            Scope = CategoryScope.Personal,
            ParentCategoryId = null
        };
    }

    public static CategoryFormModel FromCategory(
        CategoryDTO category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryFormModel
        {
            CategoryId = category.Id,
            Name = category.Name,
            Description = category.Description,
            Type = category.Type,
            Scope = category.FamilyId.HasValue
                ? CategoryScope.Family
                : CategoryScope.Personal,
            ParentCategoryId = category.ParentCategoryId
        };
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));

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