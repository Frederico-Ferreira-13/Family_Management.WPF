using System.Linq.Expressions;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class RecurringTransactionRepository
    : Repository<RecurringTransaction>,
      IRecurringTransactionRepository
{
    public RecurringTransactionRepository(
        ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(rt => rt.UserId == userId)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecurringTransaction>>
        GetActiveRecurringTransactionsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(rt =>
                rt.UserId == userId &&
                rt.IsActive)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByAccountIdAsync(Guid accountId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(rt => rt.AccountId == accountId)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecurringTransaction>>
        GetPendingGenerationAsync(DateTime asOfDate)
    {
        var exclusiveEnd = asOfDate.Date.AddDays(1);

        return await DbSet
            .AsNoTracking()
            .Where(rt =>
                rt.IsActive &&
                rt.NextDueDate < exclusiveEnd)
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }

    public Task<bool>
        RecurringTransactionExistsForUserByDescriptionAsync(
            string description,
            Guid userId)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(false);

        var normalizedDescription = description
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(rt =>
            rt.Description.ToLower() == normalizedDescription &&
            rt.UserId == userId &&
            rt.IsActive);
    }

    public Task<bool>
        RecurringTransactionExistsForUserByDescriptionAndIdAsync(
            string description,
            Guid userId,
            Guid excludeId)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Task.FromResult(false);

        var normalizedDescription = description
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(rt =>
            rt.Description.ToLower() == normalizedDescription &&
            rt.UserId == userId &&
            rt.Id != excludeId &&
            rt.IsActive);
    }

    public Task<RecurringTransaction?>
        GetRecurringTransactionByIdWithDetailsAsync(
            Guid recurringTransactionId)
    {
        return DbSet
            .AsNoTracking()
            .Include(rt => rt.User)
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .FirstOrDefaultAsync(rt =>
                rt.Id == recurringTransactionId);
    }

    public async Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByUserIdWithDetailsAsync(
            Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(rt => rt.UserId == userId)
            .Include(rt => rt.User)
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByAccountIdWithDetailsAsync(
            Guid accountId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(rt => rt.AccountId == accountId)
            .Include(rt => rt.User)
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }

    public Task<RecurringTransaction?>
        GetRecurringTransactionByIdWithGeneratedTransactionsAsync(
            Guid recurringTransactionId)
    {
        return DbSet
            .AsNoTracking()
            .Include(rt => rt.User)
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .Include(rt => rt.GeneratedTransactions)
            .FirstOrDefaultAsync(rt =>
                rt.Id == recurringTransactionId);
    }

    public async Task<IEnumerable<RecurringTransaction>>
        FindRecurringTransactionsWithDetailsAsync(
            Expression<Func<RecurringTransaction, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Include(rt => rt.User)
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .OrderBy(rt => rt.NextDueDate)
            .ToListAsync();
    }
}