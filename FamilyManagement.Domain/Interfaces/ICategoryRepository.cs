using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetCategoriesByUserIdAsync(Guid userId);
    Task<IEnumerable<Category>> GetActiveCategoriesByUserIdAsync(Guid userId);
    Task<IEnumerable<Category>> GetActiveCategoriesByFamilyIdAsync(Guid familyId);

    Task<Category?> GetByIdWithParentAsync(Guid categoryId);
    Task<Category?> GetByIdWithSubcategoriesAsync(Guid categoryId);

    Task<Category?> GetCategoryByIdWithAllHierarchyAsync(Guid categoryId);

    Task<Category?> GetCategoryByNameAndUserIdAsync(string categoryName, Guid userId);
    Task<Category?> GetByIdAndUserIdAsync(Guid categoryId, Guid userId);

    Task<IEnumerable<Category>> GetRootCategoriesByAffiliationAsync(Guid? userId, Guid? familyId, bool onlyActive = true);

    Task<IEnumerable<Category>> FindCategoriesWithDetailsAsync(Expression<Func<Category, bool>> predicate);
}