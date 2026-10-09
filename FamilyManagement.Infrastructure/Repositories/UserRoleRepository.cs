using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class UserRoleRepository
    : Repository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<UserRole?> GetByNameAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return Task.FromResult<UserRole?>(null);

        var normalizedName = roleName
            .Trim()
            .ToLowerInvariant();

        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Name.ToLower() == normalizedName &&
                r.IsActive);
    }

    public async Task<IEnumerable<UserRole>>
        GetAllActiveRolesAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public Task<bool> RoleExistsAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return Task.FromResult(false);

        var normalizedName = roleName
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(r =>
            r.Name.ToLower() == normalizedName &&
            r.IsActive);
    }
}