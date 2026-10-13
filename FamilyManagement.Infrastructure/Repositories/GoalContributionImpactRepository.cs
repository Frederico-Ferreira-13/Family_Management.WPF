using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class GoalContributionImpactRepository
    : Repository<GoalContributionImpact>,
      IGoalContributionImpactRepository
{
    public GoalContributionImpactRepository(
        ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<GoalContributionImpact?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            impact => impact.TransactionId == transactionId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<GoalContributionImpact>>
        GetAppliedByGoalIdAsync(
            Guid goalId,
            CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(impact =>
                impact.GoalId == goalId &&
                impact.IsApplied)
            .OrderByDescending(impact => impact.AppliedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasAppliedImpactAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            impact =>
                impact.TransactionId == transactionId &&
                impact.IsApplied,
            cancellationToken);
    }
}