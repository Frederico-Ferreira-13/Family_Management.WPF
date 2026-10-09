using FamilyManagement.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace FamilyManagement.Domain.Interfaces;

public interface IGoalRepository : IRepository<Goal>
{
    Task<IEnumerable<Goal>> GetGoalsByUserIdAsync(Guid userId);
    Task<IEnumerable<Goal>> GetActiveGoalsByUserIdAsync(Guid userId);

    Task<bool> GoalExistsForUserByNameAsync(string goalName, Guid userId);
    Task<bool> GoalExistsForUserByNameAndIdAsync(string goalName, Guid userId, Guid excludeGoalId);

    Task<Goal?> GetGoalByIdWithDetailsAsync(Guid goalId);
    Task<IEnumerable<Goal>> GetGoalsByUserIdWithDetailsAsync(Guid userId);

    Task<IEnumerable<Goal>> GetAchievedGoalsByUserIdAsync(Guid userId);
    Task<IEnumerable<Goal>> GetPendingGoalsByUserIdWithDetailsAsync(Guid userId);

    // Consultas Genéricas
    Task<IEnumerable<Goal>> FindGoalsWithDetailsAsync(Expression<Func<Goal, bool>> predicate);
}