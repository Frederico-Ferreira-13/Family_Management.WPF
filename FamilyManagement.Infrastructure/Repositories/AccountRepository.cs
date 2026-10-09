using System.Linq.Expressions;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class AccountRepository
    : Repository<Account>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<bool> AccountNameExistsAsync(
        string name,
        Guid userId,
        Guid accountIdToExclude)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        var normalizedName = name.Trim().ToLower();

        return DbSet.AnyAsync(a =>
            a.UserId == userId &&
            a.IsActive &&
            a.Id != accountIdToExclude &&
            a.Name.ToLower() == normalizedName);
    }

    public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetUserActiveAccountsAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetAccountsByFamilyIdAsync(
        Guid familyId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(a => a.FamilyId == familyId)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetFamilyActiveAccountsAsync(
        Guid familyId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(a =>
                a.FamilyId == familyId &&
                a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public Task<Account?> GetAccountByIdWithDetailsAsync(
        Guid accountId)
    {
        return DbSet
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a =>
                a.Id == accountId &&
                a.IsActive);
    }

    public async Task<decimal> GetTotalBalanceByUserIdAsync(
        Guid userId)
    {
        return await DbSet
            .Where(a =>
                a.UserId == userId &&
                a.IsActive)
            .SumAsync(a => a.Balance.Amount);
    }

    public async Task<IEnumerable<Account>>
        FindAccountsWithDetailsAsync(
            Expression<Func<Account, bool>> predicate)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Include(a => a.Transactions)
            .ToListAsync();
    }
}