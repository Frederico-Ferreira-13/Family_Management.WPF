using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountById;

public sealed class GetAccountByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AccountDTO>> HandleAsync(
        GetAccountByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(query.AccountId);

        if (account is null)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta não encontrada.");
        }

        return AccountMapper.ToDTO(account);
    }
}