using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AccountDTO>> HandleAsync(
        UpdateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(command.AccountId);

        if (account is null)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta não encontrada.");
        }

        var normalizedName = command.Name
            .Trim()
            .ToLowerInvariant();

        var duplicate = await _unitOfWork.Accounts.AnyAsync(a =>
            a.UserId == account.UserId &&
            a.Id != account.Id &&
            a.Name.ToLower() == normalizedName &&
            a.IsActive);

        if (duplicate)
        {
            return Error.Conflict(
                "Account.DuplicateName",
                "Já existe outra conta ativa com este nome.");
        }

        var detailsResult = account.UpdateDetails(
            command.Name,
            command.Type);

        if (detailsResult.IsFailure)
        {
            return detailsResult.Error;
        }

        var descriptionResult =
            account.UpdateDescription(command.Description);

        if (descriptionResult.IsFailure)
        {
            return descriptionResult.Error;
        }

        _unitOfWork.Accounts.Update(account);

        await _unitOfWork.CompleteAsync();

        return AccountMapper.ToDTO(account);
    }
}