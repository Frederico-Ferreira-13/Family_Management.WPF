namespace FamilyManagement.Application.UseCases.UserSettings.Commands.UpdateUserSettings;

public sealed record UpdateUserSettingsCommand(
    Guid UserId,
    Guid DefaultCurrencyId,
    string Theme,
    string Language,
    bool EnableNotifications,
    bool RequiredBiometricLogin,
    bool ShowFamilyTotalsByDefault);