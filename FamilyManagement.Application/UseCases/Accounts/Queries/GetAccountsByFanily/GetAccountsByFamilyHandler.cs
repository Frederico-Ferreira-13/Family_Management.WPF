using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByFamily;

public sealed class GetAccountsByFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountsByFamilyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<AccountDTO>>> HandleAsync(
        GetAccountsByFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        var accounts =
            await _unitOfWork.Accounts.GetAccountsByFamilyIdAsync(query.FamilyId);

        var result = accounts
            .Select(AccountMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<AccountDTO>>.Success(result);
    }
}