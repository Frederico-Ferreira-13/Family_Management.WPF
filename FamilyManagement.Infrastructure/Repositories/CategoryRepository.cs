using System.Linq.Expressions;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class CategoryRepository
    : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Category>> GetCategoriesByUserIdAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesByUserIdAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c =>
                c.UserId == userId &&
                c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesByFamilyIdAsync(
        Guid familyId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c =>
                c.FamilyId == familyId &&
                c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public Task<Category?> GetByIdWithParentAsync(Guid categoryId)
    {
        return DbSet
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public Task<Category?> GetByIdWithSubcategoriesAsync(Guid categoryId)
    {
        return DbSet
            .AsNoTracking()
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public Task<Category?> GetCategoryByIdWithAllHierarchyAsync(
        Guid categoryId)
    {
        return DbSet
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public Task<Category?> GetCategoryByNameAndUserIdAsync(
        string categoryName,
        Guid userId)
    {
        var normalizedName = categoryName.Trim().ToLower();

        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Name.ToLower() == normalizedName &&
                c.UserId == userId &&
                c.IsActive);
    }

    public Task<Category?> GetByIdAndUserIdAsync(
        Guid categoryId,
        Guid userId)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.UserId == userId);
    }

    public async Task<IEnumerable<Category>>
        GetRootCategoriesByAffiliationAsync(
            Guid? userId,
            Guid? familyId,
            bool onlyActive = true)
    {
        IQueryable<Category> query = DbSet
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == null);

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(c =>
                c.UserId == userId.Value);
        }
        else if (familyId.HasValue &&
                 familyId.Value != Guid.Empty)
        {
            query = query.Where(c =>
                c.FamilyId == familyId.Value);
        }
        else
        {
            return Array.Empty<Category>();
        }

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>>
        FindCategoriesWithDetailsAsync(
            Expression<Func<Category, bool>> predicate)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}