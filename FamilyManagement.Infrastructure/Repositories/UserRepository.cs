using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class UserRepository
    : Repository<User>, IUserRepository
{
    public UserRepository(
        ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetUserByEmailAsync(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var emailResult =
            EmailAddress.Create(email);

        if (emailResult.IsFailure)
        {
            return null;
        }

        var normalizedEmail =
            emailResult.Value;

        return await DbSet
            .FirstOrDefaultAsync(user =>
                user.Email == normalizedEmail);
    }

    public async Task<User?> GetUserByUserNameAsync(
        string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var normalizedName =
            userName
                .Trim()
                .ToLowerInvariant();

        return await DbSet
            .FirstOrDefaultAsync(user =>
                user.UserName.ToLower() ==
                normalizedName);
    }

    public Task<bool> ExistsByEmailAsync(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(false);
        }

        var emailResult =
            EmailAddress.Create(email);

        if (emailResult.IsFailure)
        {
            return Task.FromResult(false);
        }

        var normalizedEmail =
            emailResult.Value;

        return DbSet.AnyAsync(user =>
            user.Email == normalizedEmail);
    }

    public Task<bool> ExistsByUserNameAsync(
        string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult(false);
        }

        var normalizedName =
            userName
                .Trim()
                .ToLowerInvariant();

        return DbSet.AnyAsync(user =>
            user.UserName.ToLower() ==
            normalizedName);
    }

    public Task<bool> UserExistsAsync(
        Guid id)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return DbSet.AnyAsync(user =>
            user.Id == id &&
            user.IsActive);
    }

    public async Task<IEnumerable<User>>
        GetAllActiveUsersAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.UserName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>>
        GetAllUsersWithDetailsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(user => user.Family)
            .Include(user => user.Roles)
            .OrderBy(user => user.UserName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>>
        GetUsersByFamilyIdAsync(
            Guid familyId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(user =>
                user.FamilyId == familyId &&
                user.IsActive)
            .OrderBy(user => user.UserName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>>
        GetUsersWithoutFamilyAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Where(user =>
                user.FamilyId == null &&
                user.IsActive)
            .OrderBy(user => user.UserName)
            .ToListAsync();
    }

    public Task<int> CountMembersInFamilyAsync(
        Guid familyId)
    {
        return DbSet.CountAsync(user =>
            user.FamilyId == familyId &&
            user.IsActive);
    }

    public Task<User?> GetUserByIdWithFamilyAsync(
        Guid id)
    {
        return DbSet
            .AsNoTracking()
            .Include(user => user.Family)
            .FirstOrDefaultAsync(user =>
                user.Id == id &&
                user.IsActive);
    }

    public Task<User?> GetUserByIdWithSettingsAsync(
        Guid userId)
    {
        return DbSet
            .AsNoTracking()
            .Include(user => user.UserSetting)
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                user.IsActive);
    }

    public async Task<IEnumerable<User>>
        GetFamilyMembersWithDetailsAsync(
            Guid familyId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(user =>
                user.FamilyId == familyId &&
                user.IsActive)
            .Include(user => user.Family)
            .Include(user =>
                user.Accounts.Where(
                    account => account.IsActive))
            .Include(user => user.Roles)
            .OrderBy(user => user.UserName)
            .ToListAsync();
    }

    public Task<User?>
        GetUserByIdWithSettingsAndFamilyAsync(
            Guid userId)
    {
        return DbSet
            .AsNoTracking()
            .Include(user => user.UserSetting)
            .Include(user => user.Family)
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                user.IsActive);
    }

    public Task<User?>
        GetUserByIdWithFamilyAndRolesAsync(
            Guid userId)
    {
        return DbSet
            .AsNoTracking()
            .Include(user => user.Family)
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user =>
                user.Id == userId);
    }
}