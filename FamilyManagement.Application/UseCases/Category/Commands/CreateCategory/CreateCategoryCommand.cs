using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    CategoryType Type,
    Guid? UserId,
    Guid? FamilyId,
    Guid? ParentCategoryId);