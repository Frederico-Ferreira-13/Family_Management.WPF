using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.Common.Extensions;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Categories.Mapping;

public static class CategoryMapper
{
    public static CategoryDTO ToDTO(
        Category category,
        bool includeSubCategories = true)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryDTO(
            Id: category.Id,
            Name: category.Name,
            Description: category.Description,
            Type: category.Type,
            TypeDisplay: category.Type.GetDisplayName(),
            UserId: category.UserId,
            UserName: category.User?.UserName,
            FamilyId: category.FamilyId,
            FamilyName: category.Family?.Name,
            ParentCategoryId: category.ParentCategoryId,
            ParentCategoryName: category.ParentCategory?.Name,
            IsGlobal: category.IsGlobal,
            IsActive: category.IsActive,
            CreatedAt: category.CreatedAt,
            UpdatedAt: category.UpdatedAt)
        {
            SubCategories = includeSubCategories
                ? category.SubCategories
                    .Select(subCategory => ToDTO(subCategory))
                    .ToList()
                : Array.Empty<CategoryDTO>()
        };
    }

    public static CategoryLookupDTO ToLookupDTO(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryLookupDTO(
            category.Id,
            category.Name);
    }
}