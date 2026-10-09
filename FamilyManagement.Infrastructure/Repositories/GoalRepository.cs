using System.Linq.Expressions;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class GoalRepository
    : Repository<Goal>, IGoalRepository
{
    public GoalRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Goal>> GetGoalsByUserIdAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Goal>> GetActiveGoalsByUserIdAsync(
        Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(g =>
                g.UserId == userId &&
                g.IsActive)
            .OrderBy(g => g.TargetDate)
            .ThenBy(g => g.Name)
            .ToListAsync();
    }

    public Task<bool> GoalExistsForUserByNameAsync(
        string goalName,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(goalName))
            return Task.FromResult(false);

        var normalizedName = goalName
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(g =>
            g.UserId == userId &&
            g.Name.ToLower() == normalizedName &&
            g.IsActive);
    }

    public Task<bool> GoalExistsForUserByNameAndIdAsync(
        string goalName,
        Guid userId,
        Guid excludeGoalId)
    {
        if (string.IsNullOrWhiteSpace(goalName))
            return Task.FromResult(false);

        var normalizedName = goalName
            .Trim()
            .ToLowerInvariant();

        return DbSet.AnyAsync(g =>
            g.UserId == userId &&
            g.Name.ToLower() == normalizedName &&
            g.Id != excludeGoalId &&
            g.IsActive);
    }

    public Task<Goal?> GetGoalByIdWithDetailsAsync(Guid goalId)
    {
        return DbSet
            .AsNoTracking()
            .Include(g => g.User)
            .Include(g => g.Contributions)
            .FirstOrDefaultAsync(g =>
                g.Id == goalId &&
                g.IsActive);
    }

    public async Task<IEnumerable<Goal>>
        GetGoalsByUserIdWithDetailsAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .Include(g => g.Contributions)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Goal>>
        GetAchievedGoalsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(g =>
                g.UserId == userId &&
                g.IsAchieved &&
                g.IsActive)
            .OrderByDescending(g => g.UpdatedAt ?? g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Goal>>
        GetPendingGoalsByUserIdWithDetailsAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(g =>
                g.UserId == userId &&
                !g.IsAchieved &&
                g.IsActive)
            .Include(g => g.Contributions)
            .OrderBy(g => g.TargetDate)
            .ThenBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Goal>> FindGoalsWithDetailsAsync(
        Expression<Func<Goal, bool>> predicate)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Include(g => g.User)
            .Include(g => g.Contributions)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }
}