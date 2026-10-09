using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.HasActiveCategoriesForFamily;

public sealed class HasActiveCategoriesForFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public HasActiveCategoriesForFamilyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> HandleAsync(
        HasActiveCategoriesForFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        var exists = await _unitOfWork.Categories.AnyAsync(c =>
            c.FamilyId == query.FamilyId &&
            c.IsActive);

        return exists;
    }
}