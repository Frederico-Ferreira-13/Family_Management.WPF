using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class UserSettingRepository
    : Repository<UserSetting>, IUserSettingRepository
{
    public UserSettingRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public Task<UserSetting?> GetByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Task.FromResult<UserSetting?>(null);

        return DbSet.FirstOrDefaultAsync(s =>
            s.UserId == userId &&
            s.IsActive);
    }

    public Task<UserSetting?>
        GetSettingsWithCurrencyByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Task.FromResult<UserSetting?>(null);

        return DbSet
            .AsNoTracking()
            .Include(s => s.DefaultCurrency)
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.IsActive);
    }

    public Task<bool> HasSettingsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Task.FromResult(false);

        return DbSet.AnyAsync(s =>
            s.UserId == userId &&
            s.IsActive);
    }
}