using FamilyManagement.Application.UseCases.UserSettings.DTOs;
using FamilyManagement.Application.UseCases.UserSettings.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.UserSettings.Commands.UpdateUserSettings;

public sealed class UpdateUserSettingsHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserSettingsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserSettingDTO>> HandleAsync(
        UpdateUserSettingsCommand command)
    {
        var settings =
            await _unitOfWork.UserSettings
                .GetByUserIdAsync(command.UserId);

        if (settings is null)
        {
            return Error.NotFound(
                "UserSettings.NotFound",
                "Definições do utilizador não encontradas.");
        }

        var currency =
            await _unitOfWork.Currencies
                .GetByIdAsync(command.DefaultCurrencyId);

        if (currency is null || !currency.IsActive)
        {
            return Error.NotFound(
                "Currency.NotFound",
                "Moeda predefinida não encontrada.");
        }

        /*
         * Aqui chamamos o método de domínio de UserSetting.
         *
         * Não usar AutoMapper para alterar a entidade.
         *
         * Exemplo, se o Model possuir:
         *
         * var result = settings.Update(
         *     command.DefaultCurrencyId,
         *     command.Theme,
         *     command.Language,
         *     command.EnableNotifications,
         *     command.RequiredBiometricLogin,
         *     command.ShowFamilyTotalsByDefault);
         *
         * if (result.IsFailure)
         *     return result.Error;
         */

        _unitOfWork.UserSettings.Update(settings);
        await _unitOfWork.CompleteAsync();

        return UserSettingMapper.ToDTO(settings);
    }
}