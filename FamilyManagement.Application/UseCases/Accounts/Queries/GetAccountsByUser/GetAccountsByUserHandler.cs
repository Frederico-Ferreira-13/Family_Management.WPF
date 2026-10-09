using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;

public sealed class GetAccountsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<AccountDTO>>> HandleAsync(
        GetAccountsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var accounts =
            await _unitOfWork.Accounts.GetAccountsByUserIdAsync(query.UserId);

        var result = accounts
            .Select(AccountMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<AccountDTO>>.Success(result);
    }
}