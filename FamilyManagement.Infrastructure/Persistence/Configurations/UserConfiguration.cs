using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(User.MaxUserNameLength);

        // EmailAddress <-> string
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value).Value)
            .HasColumnName("Email")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FamilyId)
            .IsRequired(false);

        // Email e UserName são únicos.
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.UserName)
            .IsUnique();

        builder.HasIndex(u => u.FamilyId);

        // User -> Family
        builder.HasOne(u => u.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(u => u.FamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        // User -> UserSetting
        builder.HasOne(u => u.UserSetting)
            .WithOne(s => s.User)
            .HasForeignKey<UserSetting>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // IsAdmin é calculado a partir dos Roles.
        builder.Ignore(u => u.IsAdmin);

        // HasFamily também é calculado.
        builder.Ignore(u => u.HasFamily);

        ConfigureNavigation(builder, nameof(User.Roles));
        ConfigureNavigation(builder, nameof(User.Accounts));
        ConfigureNavigation(builder, nameof(User.Categories));
        ConfigureNavigation(builder, nameof(User.Transactions));
        ConfigureNavigation(builder, nameof(User.Budgets));
        ConfigureNavigation(builder, nameof(User.Goals));
        ConfigureNavigation(builder, nameof(User.Investments));
        ConfigureNavigation(builder, nameof(User.RecurringTransactions));
        ConfigureNavigation(builder, nameof(User.FamiliesCreated));

        // BaseEntity
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }

    private static void ConfigureNavigation(
        EntityTypeBuilder<User> builder,
        string navigationName)
    {
        builder.Metadata
            .FindNavigation(navigationName)?
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}