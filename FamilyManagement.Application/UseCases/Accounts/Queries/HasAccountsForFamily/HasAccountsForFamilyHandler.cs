using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Queries.HasAccountsForFamily;

public sealed class HasAccountsForFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public HasAccountsForFamilyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> HandleAsync(
        HasAccountsForFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        var exists = await _unitOfWork.Accounts.AnyAsync(a =>
            a.FamilyId == query.FamilyId &&
            a.IsActive);

        return Result<bool>.Success(exists);
    }
}