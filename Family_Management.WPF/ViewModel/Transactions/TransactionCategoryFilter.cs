using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Transactions;

public static class TransactionCategoryFilter
{
    public static IReadOnlyList<CategoryDTO> Filter(
        IEnumerable<CategoryDTO> categories,
        TransactionType transactionType)
    {
        ArgumentNullException.ThrowIfNull(categories);

        if (transactionType == TransactionType.Transfer)
        {
            return Array.Empty<CategoryDTO>();
        }

        var requiredType =
            TransactionFormOptions.GetCategoryType(
                transactionType);

        if (requiredType == CategoryType.NotSpecified)
        {
            return Array.Empty<CategoryDTO>();
        }

        return categories
            .Where(category =>
                category.IsActive &&
                category.Type == requiredType)
            .OrderBy(category =>
                category.ParentCategoryName)
            .ThenBy(category =>
                category.Name)
            .ToList();
    }

    public static bool IsValidSelection(
        CategoryDTO? category,
        TransactionType transactionType)
    {
        if (transactionType == TransactionType.Transfer)
        {
            return category is null;
        }

        if (category is null || !category.IsActive)
        {
            return false;
        }

        var requiredType =
            TransactionFormOptions.GetCategoryType(
                transactionType);

        return requiredType != CategoryType.NotSpecified &&
               category.Type == requiredType;
    }
}