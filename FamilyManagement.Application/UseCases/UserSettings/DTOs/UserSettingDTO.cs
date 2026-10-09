namespace FamilyManagement.Application.UseCases.UserSettings.DTOs;

public sealed record UserSettingDTO(
    Guid Id,
    Guid UserId,
    Guid DefaultCurrencyId,
    string Theme,
    string Language,
    bool EnableNotifications,
    bool RequiredBiometricLogin,
    bool ShowFamilyTotalsByDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);