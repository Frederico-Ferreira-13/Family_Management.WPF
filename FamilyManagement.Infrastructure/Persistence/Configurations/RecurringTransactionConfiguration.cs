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

        // =====================================================
        // VALOR MONETÁRIO
        // =====================================================

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

        // =====================================================
        // CONFIGURAÇÃO DA RECORRÊNCIA
        // =====================================================

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

        // =====================================================
        // IDENTIFICADORES
        // =====================================================

        builder.Property(rt => rt.CategoryId)
            .IsRequired();

        builder.Property(rt => rt.AccountId)
            .IsRequired(false);

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.BeneficiaryUserId)
            .IsRequired(false);

        builder.Property(rt => rt.FamilyId)
            .IsRequired(false);

        builder.Property(rt => rt.FinancialPlanId)
            .IsRequired(false);

        // =====================================================
        // RELAÇÕES
        // =====================================================

        // Utilizador responsável pelo registo.
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RecurringTransactions)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Membro beneficiário da receita.
        builder.HasOne(rt => rt.BeneficiaryUser)
            .WithMany(u => u.BeneficiaryRecurringTransactions)
            .HasForeignKey(rt => rt.BeneficiaryUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Família.
        builder.HasOne(rt => rt.Family)
            .WithMany()
            .HasForeignKey(rt => rt.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plano financeiro.
        builder.HasOne(rt => rt.FinancialPlan)
            .WithMany()
            .HasForeignKey(rt => rt.FinancialPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Conta bancária.
        builder.HasOne(rt => rt.Account)
            .WithMany()
            .HasForeignKey(rt => rt.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categoria.
        builder.HasOne(rt => rt.Category)
            .WithMany(c => c.RecurringTransactions)
            .HasForeignKey(rt => rt.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Movimentos gerados.
        builder.HasMany(rt => rt.GeneratedTransactions)
            .WithOne(t => t.RecurringTransaction)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(rt => rt.GeneratedTransactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // =====================================================
        // ÍNDICES
        // =====================================================

        // Motor de recorrências.
        builder.HasIndex(rt => new
        {
            rt.IsActive,
            rt.NextDueDate
        });

        // Utilizador responsável.
        builder.HasIndex(rt => rt.UserId);

        // Membro beneficiário.
        builder.HasIndex(rt => rt.BeneficiaryUserId);

        // Conta bancária.
        builder.HasIndex(rt => rt.AccountId);

        // Categoria.
        builder.HasIndex(rt => rt.CategoryId);

        // Família e próxima ocorrência.
        builder.HasIndex(rt => new
        {
            rt.FamilyId,
            rt.NextDueDate
        });

        // Plano financeiro e próxima ocorrência.
        builder.HasIndex(rt => new
        {
            rt.FinancialPlanId,
            rt.NextDueDate
        });

        // Consultas do Dashboard por beneficiário.
        builder.HasIndex(rt => new
        {
            rt.FamilyId,
            rt.BeneficiaryUserId,
            rt.IsActive
        });

        // =====================================================
        // PROPRIEDADES COMUNS
        // =====================================================

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.UpdatedAt)
            .IsRequired(false);

        builder.Property(rt => rt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }    
}