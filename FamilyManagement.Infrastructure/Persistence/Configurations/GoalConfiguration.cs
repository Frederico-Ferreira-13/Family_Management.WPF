
using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration
    : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(150);

        // TargetAmount : Money
        builder.OwnsOne(g => g.TargetAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TargetAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("TargetCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // CurrentAmount : Money
        builder.OwnsOne(g => g.CurrentAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CurrentAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("CurrentCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(g => g.StartDate)
            .IsRequired();

        builder.Property(g => g.TargetDate)
            .IsRequired(false);

        builder.Property(g => g.IsAchieved)
            .IsRequired()
            .HasDefaultValue(false);

        // Utilizador que criou a meta.
        builder.Property(g => g.UserId)
            .IsRequired();

        // Âmbito familiar opcional.
        builder.Property(g => g.FamilyId)
            .IsRequired(false);

        // Goal -> User
        builder.HasOne(g => g.User)
            .WithMany(u => u.Goals)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal -> Family
        builder.HasOne(g => g.Family)
            .WithMany()
            .HasForeignKey(g => g.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Goal -> Contributions (Transactions)
        builder.HasMany(g => g.Contributions)
            .WithOne(t => t.Goal)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(g => g.Contributions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Propriedades calculadas no domínio.
        builder.Ignore(g => g.RemainingAmount);
        builder.Ignore(g => g.ProgressPercentage);
        builder.Ignore(g => g.IsFamilyGoal);

        // BaseEntity
        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .IsRequired(false);

        builder.Property(g => g.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Consultas por utilizador.
        builder.HasIndex(g => g.UserId);

        builder.HasIndex(g => new
        {
            g.UserId,
            g.IsActive
        });

        // Consultas familiares.
        builder.HasIndex(g => g.FamilyId);

        builder.HasIndex(g => new
        {
            g.FamilyId,
            g.IsActive
        });
    }
}