
using System;
using System.Collections.Generic;
using System.Text;
using FamilyManagement.Application.UseCases.Setup.DTOs;
using FamilyManagement.Application.UseCases.Setup.Interfaces;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Setup.Services;

public sealed class SetupService : ISetupService
{
    private readonly IUnitOfWork _unitOfWork;

    public SetupService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<bool>> IsFamilySetupCompleteAsync(
        Guid familyId,
        Guid userId)
    {
        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "ID de família inválido.");
        }

        if (userId == Guid.Empty)
        {
            return Error.Validation(
                "ID de utilizador inválido.");
        }

        var hasAccounts =
            await _unitOfWork.Accounts.AnyAsync(a =>
                (a.FamilyId == familyId ||
                 a.UserId == userId) &&
                a.IsActive);

        return Result<bool>.Success(hasAccounts);
    }

    public async Task<Result<SetupStatusDTO>> GetSetupStatusAsync(
        Guid familyId)
    {
        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "ID de família inválido.");
        }

        var hasUsers =
            await _unitOfWork.Users.AnyAsync(u =>
                u.FamilyId == familyId &&
                u.IsActive);

        var hasAccounts =
            await _unitOfWork.Accounts.AnyAsync(a =>
                a.FamilyId == familyId &&
                a.IsActive);

        var hasCategories =
            await _unitOfWork.Categories.AnyAsync(c =>
                c.FamilyId == familyId &&
                c.IsActive);

        var hasGoals =
            await _unitOfWork.Goals.AnyAsync(g =>
                g.User.FamilyId == familyId &&
                g.IsActive);

        var completedSteps = 0;

        if (hasUsers) completedSteps++;
        if (hasAccounts) completedSteps++;
        if (hasCategories) completedSteps++;
        if (hasGoals) completedSteps++;

        var completionPercentage =
            completedSteps / 4.0 * 100.0;

        var status = new SetupStatusDTO
        {
            FamilyId = familyId,
            HasUsers = hasUsers,
            HasAccounts = hasAccounts,
            HasCategories = hasCategories,
            HasGoals = hasGoals,

            IsComplete =
                hasUsers &&
                hasAccounts &&
                hasCategories,

            CompletionPercentage =
                completionPercentage
        };

        return Result<SetupStatusDTO>.Success(status);
    }

    public async Task<Result<bool>>
        IsUserPersonalSetupCompleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation(
                "ID de utilizador inválido.");
        }

        var hasGoals =
            await _unitOfWork.Goals.AnyAsync(g =>
                g.UserId == userId &&
                g.IsActive);

        var hasAccounts =
            await _unitOfWork.Accounts.AnyAsync(a =>
                a.UserId == userId &&
                a.IsActive);

        return Result<bool>.Success(
            hasGoals || hasAccounts);
    }
}
