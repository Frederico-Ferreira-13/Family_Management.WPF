using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class CurrencyRepository
    : Repository<Currency>, ICurrencyRepository
{
    public CurrencyRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<Currency?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult<Currency?>(null);

        var normalizedCode = code
            .Trim()
            .ToUpperInvariant();

        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Code == normalizedCode &&
                c.IsActive);
    }

    public Task<Currency?> GetDefaultCurrencyAsync()
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IsDefault &&
                c.IsActive);
    }

    public async Task<IEnumerable<Currency>> GetAllActiveAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<IEnumerable<Currency>> GetAllCurrenciesAsync()
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public Task<Currency?> GetActiveByIdAsync(Guid currencyId)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == currencyId &&
                c.IsActive);
    }

    public Task<bool> ExistsByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult(false);

        var normalizedCode = code
            .Trim()
            .ToUpperInvariant();

        return DbSet.AnyAsync(c =>
            c.Code == normalizedCode);
    }
}