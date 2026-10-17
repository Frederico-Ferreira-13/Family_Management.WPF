using System;
using System.Threading.Tasks;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class FinancialPlanRepository
    : IFinancialPlanRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialPlanRepository(
        ApplicationDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FinancialPlan?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Set<FinancialPlan>()
            .FirstOrDefaultAsync(
                financialPlan => financialPlan.Id == id);
    }

    public async Task<FinancialPlan?> GetActiveByFamilyIdAsync(
        Guid familyId)
    {
        if (familyId == Guid.Empty)
            return null;

        return await _context.Set<FinancialPlan>()
            .AsNoTracking()
            .SingleOrDefaultAsync(financialPlan =>
                financialPlan.FamilyId == familyId &&
                financialPlan.IsActive);
    }
}
