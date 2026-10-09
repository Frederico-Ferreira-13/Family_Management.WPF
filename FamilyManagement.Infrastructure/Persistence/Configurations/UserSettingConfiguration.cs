using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class UserSettingConfiguration
    : IEntityTypeConfiguration<UserSetting>
{
    public void Configure(EntityTypeBuilder<UserSetting> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.DefaultCurrencyId)
            .IsRequired();

        builder.Property(s => s.Theme)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(UserSetting.DefaultTheme);

        builder.Property(s => s.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue(UserSetting.DefaultLanguage);

        builder.Property(s => s.EnableNotifications)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.EmailNotificationForGoals)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.RequiredBiometricLogin)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.ShowFamilyTotalsByDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // User 1 <-> 0..1 UserSetting
        builder.HasOne(s => s.User)
            .WithOne(u => u.UserSetting)
            .HasForeignKey<UserSetting>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Currency 1 -> N UserSettings
        builder.HasOne(s => s.DefaultCurrency)
            .WithMany()
            .HasForeignKey(s => s.DefaultCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Garante efetivamente a relação 1:1.
        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.HasIndex(s => s.DefaultCurrencyId);

        // BaseEntity
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired(false);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}