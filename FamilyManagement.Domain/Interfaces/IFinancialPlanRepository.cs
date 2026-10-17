
using System;
using System.Threading.Tasks;

using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IFinancialPlanRepository
{
    Task<FinancialPlan?> GetByIdAsync(Guid id);

    Task<FinancialPlan?> GetActiveByFamilyIdAsync(
        Guid familyId);
}
