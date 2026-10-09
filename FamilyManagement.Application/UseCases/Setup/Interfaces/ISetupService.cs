using FamilyManagement.Application.UseCases.Setup.DTOs;
using FamilyManagement.Domain.Common;

namespace FamilyManagement.Application.UseCases.Setup.Interfaces;

public interface ISetupService
{
    Task<Result<bool>> IsFamilySetupCompleteAsync(
        Guid familyId,
        Guid userId);

    Task<Result<SetupStatusDTO>> GetSetupStatusAsync(
        Guid familyId);

    Task<Result<bool>> IsUserPersonalSetupCompleteAsync(
        Guid userId);
}