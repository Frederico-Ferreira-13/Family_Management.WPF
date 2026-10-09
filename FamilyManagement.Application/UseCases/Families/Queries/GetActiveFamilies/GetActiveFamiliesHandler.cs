using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Queries.GetActiveFamilies;

public sealed class GetActiveFamiliesHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveFamiliesHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result<IReadOnlyCollection<FamilyDTO>>> HandleAsync(
        GetActiveFamiliesQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var families = await _unitOfWork.Families
            .GetAllActiveFamiliesAsync();

        cancellationToken.ThrowIfCancellationRequested();

        var result = families
            .OrderBy(family => family.Name)
            .Select(FamilyMapper.ToDTO)
            .ToList();

        return Result<IReadOnlyCollection<FamilyDTO>>.Success(
            result);
    }
}