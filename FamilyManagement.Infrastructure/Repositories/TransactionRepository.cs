using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class TransactionRepository
    : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<Transaction?> GetByIdWithDetailsAsync(Guid id)
    {
        return ApplyDetails(
                DbSet.AsNoTracking())
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.IsActive);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(
        Guid userId,
        int? limit = null)
    {
        IQueryable<Transaction> query = DbSet
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.IsActive)
            .OrderByDescending(t => t.Date);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(
        Guid accountId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t =>
                (t.AccountId == accountId ||
                 t.TargetAccountId == accountId) &&
                t.IsActive)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(
        Guid userId,
        DateTime start,
        DateTime end)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.IsActive &&
                t.Date >= start &&
                t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>>
        GetByUserIdWithDetailsAsync(
            Guid userId,
            TransactionFilter? filter = null)
    {
        filter ??= new TransactionFilter();

        filter.UserId = userId;

        var query = ApplyFilter(filter)
            .AsNoTracking();

        if (filter.IncludeDetails)
            query = ApplyDetails(query);

        if (filter.Limit.HasValue)
            query = query.Take(filter.Limit.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>>
        GetByFamilyIdWithDetailsAsync(
            Guid familyId,
            DateTime start,
            DateTime end)
    {
        return await ApplyDetails(
                DbSet.AsNoTracking())
            .Where(t =>
                t.User != null &&
                t.User.FamilyId == familyId &&
                t.IsActive &&
                t.Date >= start &&
                t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> SearchAsync(
        Guid userId,
        string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Array.Empty<Transaction>();

        var filter = new TransactionFilter
        {
            UserId = userId,
            SearchTerm = term.Trim(),
            IncludeDetails = true
        };

        return await ApplyDetails(
                ApplyFilter(filter).AsNoTracking())
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetUnconfirmedAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsConfirmed &&
                t.IsActive)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalAmountByTypeAsync(
        Guid userId,
        TransactionType type,
        DateTime start,
        DateTime end)
    {
        return await DbSet
            .Where(t =>
                t.UserId == userId &&
                t.Type == type &&
                t.IsActive &&
                t.IsConfirmed &&
                t.Date >= start &&
                t.Date <= end)
            .SumAsync(t => t.Amount.Amount);
    }

    public async Task<decimal> GetAccountBalanceAsOfDateAsync(
        Guid accountId,
        DateTime asOfDate)
    {
        var exclusiveEnd =
            asOfDate.Date.AddDays(1);

        var totalIn = await DbSet
            .Where(t =>
                t.IsActive &&
                t.IsConfirmed &&
                t.Date < exclusiveEnd &&
                (
                    (t.AccountId == accountId &&
                     t.Type == TransactionType.Income)
                    ||
                    (t.TargetAccountId == accountId &&
                     t.Type == TransactionType.Transfer)
                ))
            .SumAsync(t => t.Amount.Amount);

        var totalOut = await DbSet
            .Where(t =>
                t.IsActive &&
                t.IsConfirmed &&
                t.Date < exclusiveEnd &&
                t.AccountId == accountId &&
                (
                    t.Type == TransactionType.Expense ||
                    t.Type == TransactionType.Transfer
                ))
            .SumAsync(t => t.Amount.Amount);

        return totalIn - totalOut;
    }

    private IQueryable<Transaction> ApplyFilter(
        TransactionFilter filter)
    {
        var query = DbSet.Where(t => t.IsActive);

        if (filter.UserId.HasValue)
        {
            query = query.Where(t =>
                t.UserId == filter.UserId.Value);
        }

        if (filter.AccountId.HasValue)
        {
            query = query.Where(t =>
                t.AccountId == filter.AccountId.Value ||
                t.TargetAccountId == filter.AccountId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == filter.CategoryId.Value);
        }

        if (filter.BudgetId.HasValue)
        {
            query = query.Where(t =>
                t.BudgetId == filter.BudgetId.Value);
        }

        if (filter.GoalId.HasValue)
        {
            query = query.Where(t =>
                t.GoalId == filter.GoalId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t =>
                t.Type == filter.Type.Value);
        }

        if (filter.IsConfirmed.HasValue)
        {
            query = query.Where(t =>
                t.IsConfirmed == filter.IsConfirmed.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t =>
                t.Date >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t =>
                t.Date <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm
                .Trim()
                .ToLowerInvariant();

            query = query.Where(t =>
                t.Description.ToLower().Contains(term) ||
                (t.Notes != null &&
                 t.Notes.ToLower().Contains(term)));
        }

        return query;
    }

    private static IQueryable<Transaction> ApplyDetails(
        IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.TargetAccount)
            .Include(t => t.Budget)
            .Include(t => t.Goal)
            .Include(t => t.RecurringTransaction)
            .Include(t => t.User);
    }
}