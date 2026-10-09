using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Categories.DTOs;

public sealed record CategoryDTO(
    Guid Id,
    string Name,
    string? Description,
    CategoryType Type,
    string TypeDisplay,
    Guid? UserId,
    string? UserName,
    Guid? FamilyId,
    string? FamilyName,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    bool IsGlobal,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public IReadOnlyCollection<CategoryDTO> SubCategories { get; init; }
        = Array.Empty<CategoryDTO>();
}