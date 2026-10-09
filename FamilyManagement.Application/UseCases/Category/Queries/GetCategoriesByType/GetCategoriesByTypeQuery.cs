using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetCategoriesByType;

public sealed record GetCategoriesByTypeQuery(
    Guid FamilyId,
    CategoryType Type);