using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;

public sealed class GetAccountsByUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountsByUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<IEnumerable<AccountDTO>>> HandleAsync(
        GetAccountsByUserQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (query.UserId == Guid.Empty)
        {
            return Error.BusinessRule(
                "Account.InvalidUser",
                "É necessário identificar o utilizador.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(query.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.IsActive)
        {
            return Error.BusinessRule(
                "User.Inactive",
                "Um utilizador inativo não pode consultar contas.");
        }

        IEnumerable<FamilyManagement.Domain.Model.Account> accounts;

        if (query.FamilyId.HasValue)
        {
            if (query.FamilyId.Value == Guid.Empty)
            {
                return Error.BusinessRule(
                    "Family.InvalidId",
                    "O identificador da família é inválido.");
            }

            if (user.FamilyId != query.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Account.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(query.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "A família não existe ou está inativa.");
            }

            accounts = await _unitOfWork.Accounts
                .GetAccountsByFamilyIdAsync(query.FamilyId.Value);
        }
        else
        {
            accounts = await _unitOfWork.Accounts
                .GetAccountsByUserIdAsync(query.UserId);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = accounts
            .Select(AccountMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<AccountDTO>>.Success(result);
    }
}