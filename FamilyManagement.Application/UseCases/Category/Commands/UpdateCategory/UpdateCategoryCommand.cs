using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    CategoryType Type,
    Guid? ParentCategoryId);