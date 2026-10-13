
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
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
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Name)
            .ToListAsync();
    }    

    public Task<Budget?>
        GetBudgetsByCategoryIdAndUserIdAndMonthYearAsync(
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
                b.FamilyId == null &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive);
    }

    public async Task<IEnumerable<Budget>>
        GetBudgetsByUserAndMonthYearAsync(
            Guid userId,
            int month,
            int year)
    {
        return await DbSet
            .AsNoTracking()
            .Where(b =>
                b.UserId == userId &&
                b.FamilyId == null &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();
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
                b.FamilyId == null &&
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

    public Task<Budget?> GetBudgetByFamilyCategoryAndPeriodAsync(
        Guid familyId,
        Guid categoryId,
        int month,
        int year)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.FamilyId == familyId &&
                b.CategoryId == categoryId &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive);
    }

    public async Task<IEnumerable<Budget>>
        GetBudgetsByFamilyAndMonthYearAsync(
            Guid familyId,
            int month,
            int year)
    {
        return await DbSet
            .AsNoTracking()
            .Where(b =>
                b.FamilyId == familyId &&
                b.Month == month &&
                b.Year == year &&
                b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<(decimal TotalBudgeted, decimal TotalSpent)>
        GetFamilyMonthlySummaryAsync(
            Guid familyId,
            int month,
            int year)
    {
        var budgets = await DbSet
            .AsNoTracking()
            .Where(b =>
                b.FamilyId == familyId &&
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

    public Task<Budget?> GetBudgetByIdWithDetailsAsync(
        Guid budgetId)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == budgetId &&
                b.IsActive);
    }
}