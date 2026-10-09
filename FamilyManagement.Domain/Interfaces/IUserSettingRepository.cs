using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IUserSettingRepository : IRepository<UserSetting>
{
    Task<UserSetting?> GetByUserIdAsync(Guid userId);

    Task<UserSetting?> GetSettingsWithCurrencyByUserIdAsync(
        Guid userId);

    Task<bool> HasSettingsAsync(Guid userId);
}