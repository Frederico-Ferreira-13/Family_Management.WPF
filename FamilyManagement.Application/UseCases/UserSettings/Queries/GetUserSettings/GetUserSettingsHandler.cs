using FamilyManagement.Application.UseCases.UserSettings.DTOs;
using FamilyManagement.Application.UseCases.UserSettings.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.UserSettings.Queries.GetUserSettings;

public sealed class GetUserSettingsHandler
{
    private readonly IUserSettingRepository _repository;

    public GetUserSettingsHandler(
        IUserSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UserSettingDTO>> HandleAsync(
        GetUserSettingsQuery query)
    {
        if (query.UserId == Guid.Empty)
            return Error.Validation(
                "O utilizador é obrigatório.");

        var settings =
            await _repository.GetByUserIdAsync(query.UserId);

        if (settings is null)
        {
            return Error.NotFound(
                "UserSettings.NotFound",
                "Definições do utilizador não encontradas.");
        }

        return UserSettingMapper.ToDTO(settings);
    }
}