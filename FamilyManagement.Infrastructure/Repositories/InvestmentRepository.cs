using System.Linq.Expressions;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class InvestmentRepository
    : Repository<Investment>, IInvestmentRepository
{
    public InvestmentRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Investment>>
        GetInvestmentsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        GetActiveInvestmentsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(i =>
                i.UserId == userId &&
                i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        GetInvestmentsByAccountIdAsync(Guid accountId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(i => i.AccountId == accountId)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public Task<Investment?> GetInvestmentByNameAndUserIdAsync(
        string name,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult<Investment?>(null);

        var normalizedName = name
            .Trim()
            .ToLowerInvariant();

        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Name.ToLower() == normalizedName &&
                i.UserId == userId &&
                i.IsActive);
    }

    public Task<bool> InvestmentExistsForUserByNameAsync(
        string name,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        var normalizedName = name
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(i =>
            i.Name.ToLower() == normalizedName &&
            i.UserId == userId &&
            i.IsActive);
    }

    public Task<bool> InvestmentExistsForUserByNameAndIdAsync(
        string name,
        Guid userId,
        Guid excludeInvestmentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        var normalizedName = name
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(i =>
            i.Name.ToLower() == normalizedName &&
            i.UserId == userId &&
            i.Id != excludeInvestmentId &&
            i.IsActive);
    }

    public Task<Investment?> GetInvestmentByIdWithDetailsAsync(
        Guid investmentId)
    {
        return DbSet
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Account)
            .FirstOrDefaultAsync(i =>
                i.Id == investmentId &&
                i.IsActive);
    }

    public async Task<IEnumerable<Investment>>
        GetInvestmentsByUserIdWithDetailsAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Account)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CurrentValue.Amount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        GetActiveInvestmentsByUserIdWithDetailsAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Account)
            .Where(i =>
                i.UserId == userId &&
                i.IsActive)
            .OrderByDescending(i => i.CurrentValue.Amount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        GetInvestmentsByAccountIdWithDetailsAsync(Guid accountId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(i => i.Account)
            .Where(i => i.AccountId == accountId)
            .OrderByDescending(i => i.CurrentValue.Amount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        FindInvestmentsWithDetailsAsync(
            Expression<Func<Investment, bool>> predicate)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Include(i => i.User)
            .Include(i => i.Account)
            .OrderByDescending(i => i.CurrentValue.Amount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        GetProfitableInvestmentsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(i =>
                i.UserId == userId &&
                i.IsActive &&
                i.InitialValue.Amount > 0 &&
                i.CurrentValue.Amount > i.InitialValue.Amount)
            .Include(i => i.Account)
            .OrderByDescending(i =>
                (i.CurrentValue.Amount - i.InitialValue.Amount)
                / i.InitialValue.Amount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Investment>>
        GetLossMakingInvestmentsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(i =>
                i.UserId == userId &&
                i.IsActive &&
                i.InitialValue.Amount > 0 &&
                i.CurrentValue.Amount < i.InitialValue.Amount)
            .Include(i => i.Account)
            .OrderBy(i =>
                (i.CurrentValue.Amount - i.InitialValue.Amount)
                / i.InitialValue.Amount)
            .ToListAsync();
    }
}