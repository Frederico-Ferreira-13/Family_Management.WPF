using System.Linq.Expressions;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class FamilyRepository
    : Repository<Family>, IFamilyRepository
{
    public FamilyRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<Family?> GetFamilyByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult<Family?>(null);

        var normalizedName = name
            .Trim()
            .ToLowerInvariant();

        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(f =>
                f.Name.ToLower() == normalizedName &&
                f.IsActive);
    }

    public async Task<IEnumerable<Family>> GetAllActiveFamiliesAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public Task<Family?> GetByIdWithDetailsAsync(Guid familyId)
    {
        return DbSet
            .AsNoTracking()
            .Include(f => f.CreatorUser)
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f =>
                f.Id == familyId &&
                f.IsActive);
    }

    public async Task<IEnumerable<Family>> FindFamiliesWithDetailsAsync(
        Expression<Func<Family, bool>> predicate)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Include(f => f.CreatorUser)
            .Include(f => f.Members)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public Task<bool> IsUserMemberOfFamilyAsync(
        Guid familyId,
        Guid userId)
    {
        return DbSet.AnyAsync(f =>
            f.Id == familyId &&
            f.IsActive &&
            f.Members.Any(u =>
                u.Id == userId &&
                u.IsActive));
    }

    public Task<bool> IsUserCreatorOfFamilyAsync(
        Guid familyId,
        Guid userId)
    {
        return DbSet.AnyAsync(f =>
            f.Id == familyId &&
            f.CreatorUserId == userId &&
            f.IsActive);
    }

    public Task<Family?> GetFamilyByInvitationCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult<Family?>(null);

        var normalizedCode = code
            .Trim()
            .ToUpperInvariant();

        return DbSet
            .AsNoTracking()
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f =>
                f.InvitationCode == normalizedCode &&
                f.IsActive);
    }

    public Task<bool> FamilyNameExistsAsync(
        string name,
        Guid? excludeFamilyId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        var normalizedName = name
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(f =>
            f.Name.ToLower() == normalizedName &&
            f.IsActive &&
            (!excludeFamilyId.HasValue ||
             f.Id != excludeFamilyId.Value));
    }
}