using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration
    : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        // Amount : Money
        builder.OwnsOne(t => t.Amount, money =>
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

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(t => t.Type)
            .IsRequired();

        builder.Property(t => t.Date)
            .IsRequired();

        builder.Property(t => t.IsConfirmed)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.IsRecurringGenerated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.CategoryId)
            .IsRequired(false);

        builder.Property(t => t.TargetAccountId)
            .IsRequired(false);

        builder.Property(t => t.RecurringTransactionId)
            .IsRequired(false);

        builder.Property(t => t.BudgetId)
            .IsRequired(false);

        builder.Property(t => t.GoalId)
            .IsRequired(false);

        // User -> Transactions
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Account -> Transactions
        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> Transactions
        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Budget não possui ICollection<Transaction>.
        builder.HasOne(t => t.Budget)
            .WithMany()
            .HasForeignKey(t => t.BudgetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Goal -> Contributions
        builder.HasOne(t => t.Goal)
            .WithMany(g => g.Contributions)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        // RecurringTransaction -> GeneratedTransactions
        builder.HasOne(t => t.RecurringTransaction)
            .WithMany(rt => rt.GeneratedTransactions)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Conta de destino de uma transferência.
        builder.HasOne(t => t.TargetAccount)
            .WithMany()
            .HasForeignKey(t => t.TargetAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.BudgetId);
        builder.HasIndex(t => t.GoalId);
        builder.HasIndex(t => t.RecurringTransactionId);

        // Extrato de uma conta.
        builder.HasIndex(t => new
        {
            t.AccountId,
            t.Date
        });

        builder.HasIndex(t => new
        {
            t.UserId,
            t.Date
        });

        builder.HasIndex(t => t.Type);

        // BaseEntity
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired(false);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}