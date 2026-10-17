
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

        // Valor monetário
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

        // Categoria obrigatória
        builder.Property(rt => rt.CategoryId)
            .IsRequired();

        // Conta bancária opcional
        builder.Property(rt => rt.AccountId)
            .IsRequired(false);

        // Utilizador responsável
        builder.Property(rt => rt.UserId)
            .IsRequired();

        // Âmbito familiar opcional
        builder.Property(rt => rt.FamilyId)
            .IsRequired(false);

        // Plano financeiro opcional para manter
        // compatibilidade com recorrências existentes
        builder.Property(rt => rt.FinancialPlanId)
            .IsRequired(false);

        // Utilizador -> Recorrências
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RecurringTransactions)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Família -> Recorrências
        builder.HasOne(rt => rt.Family)
            .WithMany()
            .HasForeignKey(rt => rt.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plano financeiro -> Recorrências
        builder.HasOne(rt => rt.FinancialPlan)
            .WithMany()
            .HasForeignKey(rt => rt.FinancialPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Conta bancária -> Recorrências
        builder.HasOne(rt => rt.Account)
            .WithMany()
            .HasForeignKey(rt => rt.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categoria -> Recorrências
        builder.HasOne(rt => rt.Category)
            .WithMany(c => c.RecurringTransactions)
            .HasForeignKey(rt => rt.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Recorrência -> Movimentos gerados
        builder.HasMany(rt => rt.GeneratedTransactions)
            .WithOne(t => t.RecurringTransaction)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(rt => rt.GeneratedTransactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Índice utilizado pelo motor de recorrências
        builder.HasIndex(rt => new
        {
            rt.IsActive,
            rt.NextDueDate
        });

        // Pesquisa por utilizador
        builder.HasIndex(rt => rt.UserId);

        // Pesquisa por conta bancária
        builder.HasIndex(rt => rt.AccountId);

        // Pesquisa por categoria
        builder.HasIndex(rt => rt.CategoryId);

        // Pesquisa por família
        builder.HasIndex(rt => new
        {
            rt.FamilyId,
            rt.NextDueDate
        });

        // Pesquisa por plano financeiro
        builder.HasIndex(rt => new
        {
            rt.FinancialPlanId,
            rt.NextDueDate
        });

        // Propriedades comuns
        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.UpdatedAt)
            .IsRequired(false);

        builder.Property(rt => rt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
