using System.Linq.Expressions;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class BudgetRepository
    : Repository<Budget>, IBudgetRepository
{
    public BudgetRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Budget>> FindBudgetsWithDetailsAsync(
        Expression<Func<Budget, bool>> predicate)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Name)
            .ToListAsync();
    }

    public Task<Budget?> GetBudgetsByCategoryIdAndUserIdAndMonthYearAsync(
        Guid categoryId,
        Guid userId,
        int month,
        int year)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.CategoryId == categoryId &&
                b.UserId == userId &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive);
    }

    public async Task<IEnumerable<Budget>> GetBudgetsByUserAndMonthYearAsync(
        Guid userId,
        int month,
        int year)
    {
        return await DbSet
            .AsNoTracking()
            .Where(b =>
                b.UserId == userId &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public Task<Budget?> GetBudgetByIdWithDetailsAsync(Guid budgetId)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == budgetId &&
                b.IsActive);
    }

    public async Task<(decimal TotalBudgeted, decimal TotalSpent)>
        GetMonthlySummaryAsync(
            Guid userId,
            int month,
            int year)
    {
        var budgets = await DbSet
            .AsNoTracking()
            .Where(b =>
                b.UserId == userId &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive)
            .Select(b => new
            {
                Budgeted = b.BudgetedAmount.Amount,
                Spent = b.CurrentSpent.Amount
            })
            .ToListAsync();

        return (
            budgets.Sum(b => b.Budgeted),
            budgets.Sum(b => b.Spent));
    }
}