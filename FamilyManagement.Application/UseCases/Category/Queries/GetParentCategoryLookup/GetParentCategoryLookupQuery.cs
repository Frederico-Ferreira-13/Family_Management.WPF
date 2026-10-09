using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetParentCategoryLookup;

public sealed record GetParentCategoryLookupQuery(
    Guid? UserId,
    Guid? FamilyId,
    CategoryType Type,
    Guid? ExcludeCategoryId = null);