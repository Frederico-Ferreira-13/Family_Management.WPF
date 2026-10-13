using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IGoalContributionImpactRepository
    : IRepository<GoalContributionImpact>
{
    Task<GoalContributionImpact?> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoalContributionImpact>> GetAppliedByGoalIdAsync(
        Guid goalId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAppliedImpactAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
