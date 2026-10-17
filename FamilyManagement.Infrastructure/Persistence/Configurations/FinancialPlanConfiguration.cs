using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class FinancialPlanConfiguration
    : IEntityTypeConfiguration<FinancialPlan>
{
    public void Configure(
        EntityTypeBuilder<FinancialPlan> builder)
    {
        builder.ToTable("FinancialPlans");

        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(plan => plan.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(plan => plan.Description)
            .HasMaxLength(500);

        builder.Property(plan => plan.FamilyId)
            .IsRequired();

        builder.Property(plan => plan.CreatedByUserId)
            .IsRequired();

        builder.HasOne(plan => plan.Family)
            .WithMany()
            .HasForeignKey(plan => plan.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(plan => plan.CreatedByUser)
            .WithMany()
            .HasForeignKey(plan => plan.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(plan => plan.CreatedAt)
            .IsRequired();

        builder.Property(plan => plan.UpdatedAt)
            .IsRequired(false);

        builder.Property(plan => plan.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(plan => plan.FamilyId);

        builder.HasIndex(plan => plan.CreatedByUserId);

        builder.HasIndex(plan => new
        {
            plan.FamilyId,
            plan.Name
        });
    }
}