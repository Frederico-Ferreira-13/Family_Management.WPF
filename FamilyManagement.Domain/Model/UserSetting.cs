using System;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Model;

public class UserSetting : BaseEntity
{
    public const string DefaultTheme = "system";
    public const string DefaultLanguage = "pt-PT";

    public Guid UserId { get; private set; }

    public virtual User User { get; private set; } = null!;

    public Guid DefaultCurrencyId { get; private set; }

    public virtual Currency DefaultCurrency { get; private set; } =
        null!;

    public string Theme { get; private set; } = DefaultTheme;
    public string Language { get; private set; } = DefaultLanguage;

    public bool EnableNotifications { get; private set; }
    public bool EmailNotificationForGoals { get; private set; }

    public bool RequiredBiometricLogin { get; private set; }

    public bool ShowFamilyTotalsByDefault { get; private set; }

    private UserSetting()
    {
    }

    private UserSetting(
        Guid userId,
        Guid defaultCurrencyId)
    {
        UserId = userId;
        DefaultCurrencyId = defaultCurrencyId;

        EnableNotifications = true;
    }

    public static Result<UserSetting> Create(
        Guid userId,
        Guid defaultCurrencyId)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (defaultCurrencyId == Guid.Empty)
        {
            return Error.Validation(
                "A moeda padrão é obrigatória.");
        }

        return new UserSetting(
            userId,
            defaultCurrencyId);
    }

    public Result UpdateDefaultCurrency(Guid currencyId)
    {
        if (!IsActive)
            return InactiveError();

        if (currencyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da moeda é obrigatório.");
        }

        if (DefaultCurrencyId == currencyId)
            return Result.Success();

        DefaultCurrencyId = currencyId;
        Touch();

        return Result.Success();
    }

    public Result UpdateTheme(string newTheme)
    {
        if (!IsActive)
            return InactiveError();

        if (string.IsNullOrWhiteSpace(newTheme))
        {
            return Error.Validation(
                "O tema é obrigatório.");
        }

        var normalizedTheme = newTheme
            .Trim()
            .ToLowerInvariant();

        if (normalizedTheme is not ("system" or "light" or "dark"))
        {
            return Error.Validation(
                "O tema deve ser system, light ou dark.");
        }

        if (Theme == normalizedTheme)
            return Result.Success();

        Theme = normalizedTheme;
        Touch();

        return Result.Success();
    }

    public Result UpdateLanguage(string newLanguage)
    {
        if (!IsActive)
            return InactiveError();

        if (string.IsNullOrWhiteSpace(newLanguage))
        {
            return Error.Validation(
                "O idioma é obrigatório.");
        }

        var normalizedLanguage = newLanguage.Trim();

        if (Language == normalizedLanguage)
            return Result.Success();

        Language = normalizedLanguage;
        Touch();

        return Result.Success();
    }

    public Result ToggleBiometricLogin(bool required)
    {
        if (!IsActive)
            return InactiveError();

        if (RequiredBiometricLogin == required)
            return Result.Success();

        RequiredBiometricLogin = required;
        Touch();

        return Result.Success();
    }

    public Result UpdateNotificationPreferences(
        bool enableNotifications,
        bool emailForGoals)
    {
        if (!IsActive)
            return InactiveError();

        if (EnableNotifications == enableNotifications &&
            EmailNotificationForGoals == emailForGoals)
        {
            return Result.Success();
        }

        EnableNotifications = enableNotifications;
        EmailNotificationForGoals = emailForGoals;

        Touch();

        return Result.Success();
    }

    public Result ToggleFamilyTotals(bool showByDefault)
    {
        if (!IsActive)
            return InactiveError();

        if (ShowFamilyTotalsByDefault == showByDefault)
            return Result.Success();

        ShowFamilyTotalsByDefault = showByDefault;
        Touch();

        return Result.Success();
    }

    private static Error InactiveError()
    {
        return Error.Conflict(
            "user_settings.inactive",
            "As definições do utilizador estão inativas.");
    }
}