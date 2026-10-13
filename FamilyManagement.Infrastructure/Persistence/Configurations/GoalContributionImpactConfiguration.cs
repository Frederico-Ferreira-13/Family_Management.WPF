
using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class GoalContributionImpactConfiguration
    : IEntityTypeConfiguration<GoalContributionImpact>
{
    public void Configure(
        EntityTypeBuilder<GoalContributionImpact> builder)
    {
        builder.ToTable("GoalContributionImpacts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionId)
            .IsRequired();

        builder.Property(x => x.GoalId)
            .IsRequired();

        builder.Property(x => x.AppliedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.IsApplied)
            .IsRequired();

        builder.Property(x => x.AppliedAt);
        builder.Property(x => x.RevertedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Uma transação só pode ter um registo
        // de impacto sobre metas.
        builder.HasIndex(x => x.TransactionId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.GoalId,
            x.IsApplied
        });

        builder.HasOne(x => x.Transaction)
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Goal)
            .WithMany()
            .HasForeignKey(x => x.GoalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
