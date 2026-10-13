
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IBudgetRepository : IRepository<Budget>
{
    Task<IEnumerable<Budget>> FindBudgetsWithDetailsAsync(
        Expression<Func<Budget, bool>> predicate);

    Task<Budget?> GetBudgetsByCategoryIdAndUserIdAndMonthYearAsync(
        Guid categoryId,
        Guid userId,
        int month,
        int year);

    Task<IEnumerable<Budget>> GetBudgetsByUserAndMonthYearAsync(
        Guid userId,
        int month,
        int year);

    Task<Budget?> GetBudgetByIdWithDetailsAsync(
        Guid budgetId);

    Task<(decimal TotalBudgeted, decimal TotalSpent)>
        GetMonthlySummaryAsync(
            Guid userId,
            int month,
            int year);

    // Consultas específicas do âmbito familiar.

    Task<Budget?> GetBudgetByFamilyCategoryAndPeriodAsync(
        Guid familyId,
        Guid categoryId,
        int month,
        int year);

    Task<IEnumerable<Budget>> GetBudgetsByFamilyAndMonthYearAsync(
        Guid familyId,
        int month,
        int year);

    Task<(decimal TotalBudgeted, decimal TotalSpent)>
        GetFamilyMonthlySummaryAsync(
            Guid familyId,
            int month,
            int year);
}