using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class RecurringTransactionConfiguration
    : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(
        EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.ToTable("RecurringTransactions");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Description)
            .IsRequired()
            .HasMaxLength(200);

        // Amount : Money
        builder.OwnsOne(rt => rt.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(rt => rt.Type)
            .IsRequired();

        builder.Property(rt => rt.Frequency)
            .IsRequired();

        builder.Property(rt => rt.StartDate)
            .IsRequired();

        builder.Property(rt => rt.EndDate)
            .IsRequired(false);

        builder.Property(rt => rt.LastGenerateDate)
            .IsRequired(false);

        builder.Property(rt => rt.NextDueDate)
            .IsRequired();

        builder.Property(rt => rt.CategoryId)
            .IsRequired();

        builder.Property(rt => rt.AccountId)
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        // RecurringTransaction -> User
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RecurringTransactions)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // RecurringTransaction -> Account
        //
        // Account não possui RecurringTransactions.
        builder.HasOne(rt => rt.Account)
            .WithMany()
            .HasForeignKey(rt => rt.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // RecurringTransaction -> Category
        builder.HasOne(rt => rt.Category)
            .WithMany(c => c.RecurringTransactions)
            .HasForeignKey(rt => rt.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // RecurringTransaction -> Transactions geradas
        builder.HasMany(rt => rt.GeneratedTransactions)
            .WithOne(t => t.RecurringTransaction)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(rt => rt.GeneratedTransactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Pesquisa frequente do motor de recorrências.
        builder.HasIndex(rt => new
        {
            rt.IsActive,
            rt.NextDueDate
        });

        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.AccountId);
        builder.HasIndex(rt => rt.CategoryId);

        // BaseEntity
        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.UpdatedAt)
            .IsRequired(false);

        builder.Property(rt => rt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}