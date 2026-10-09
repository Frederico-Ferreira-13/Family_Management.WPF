using FamilyManagement.Application.UseCases.UserSettings.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.UserSettings.Mapping;

public static class UserSettingMapper
{
    public static UserSettingDTO ToDTO(UserSetting settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new UserSettingDTO(
            settings.Id,
            settings.UserId,
            settings.DefaultCurrencyId,
            settings.Theme,
            settings.Language,
            settings.EnableNotifications,
            settings.RequiredBiometricLogin,
            settings.ShowFamilyTotalsByDefault,
            settings.IsActive,
            settings.CreatedAt,
            settings.UpdatedAt);
    }
}