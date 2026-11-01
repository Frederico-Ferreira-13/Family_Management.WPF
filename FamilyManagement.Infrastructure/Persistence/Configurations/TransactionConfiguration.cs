
using FamilyManagement.Domain.Enums;
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

        // =====================================================
        // VALOR MONETÁRIO
        // =====================================================

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

        // =====================================================
        // DADOS DO MOVIMENTO
        // =====================================================

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

        // =====================================================
        // RESPONSÁVEL
        // =====================================================

        builder.Property(t => t.UserId)
            .IsRequired();

        // =====================================================
        // CLASSIFICAÇÃO FINANCEIRA
        // =====================================================

        builder.Property(t => t.Scope)
            .HasConversion<int>()
            .HasColumnName("Scope")
            .HasDefaultValue(RecurringTransactionScope.Personal)
            .IsRequired();

        builder.Property(t => t.FamilyId)
            .IsRequired(false);

        builder.Property(t => t.BeneficiaryUserId)
            .IsRequired(false);

        builder.Property(t => t.FinancialPlanId)
            .IsRequired(false);

        // =====================================================
        // CONTAS E ASSOCIAÇÕES
        // =====================================================

        builder.Property(t => t.AccountId)
            .IsRequired(false);

        builder.Property(t => t.TargetAccountId)
            .IsRequired(false);

        builder.Property(t => t.CategoryId)
            .IsRequired(false);

        builder.Property(t => t.RecurringTransactionId)
            .IsRequired(false);

        builder.Property(t => t.BudgetId)
            .IsRequired(false);

        builder.Property(t => t.GoalId)
            .IsRequired(false);

        // =====================================================
        // RELAÇÕES
        // =====================================================

        // Utilizador responsável pelo movimento.
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Família associada ao âmbito financeiro.
        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Beneficiário de receitas familiares.
        builder.HasOne(t => t.BeneficiaryUser)
            .WithMany()
            .HasForeignKey(t => t.BeneficiaryUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plano financeiro.
        builder.HasOne(t => t.FinancialPlan)
            .WithMany()
            .HasForeignKey(t => t.FinancialPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Conta bancária de origem.
        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Conta de destino das transferências.
        builder.HasOne(t => t.TargetAccount)
            .WithMany()
            .HasForeignKey(t => t.TargetAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categoria.
        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Orçamento.
        builder.HasOne(t => t.Budget)
            .WithMany()
            .HasForeignKey(t => t.BudgetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Objetivo.
        builder.HasOne(t => t.Goal)
            .WithMany(g => g.Contributions)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        // Recorrência que originou o movimento.
        builder.HasOne(t => t.RecurringTransaction)
            .WithMany(rt => rt.GeneratedTransactions)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // =====================================================
        // ÍNDICES
        // =====================================================

        builder.HasIndex(t => t.Date);

        builder.HasIndex(t => t.UserId);

        builder.HasIndex(t => t.CategoryId);

        builder.HasIndex(t => t.BudgetId);

        builder.HasIndex(t => t.GoalId);

        builder.HasIndex(t => t.RecurringTransactionId);

        builder.HasIndex(t => t.Type);

        builder.HasIndex(t => t.Scope);

        builder.HasIndex(t => t.BeneficiaryUserId);

        // Extrato bancário.
        builder.HasIndex(t => new
        {
            t.AccountId,
            t.Date
        });

        // Histórico pessoal por utilizador.
        builder.HasIndex(t => new
        {
            t.UserId,
            t.Date
        });

        // Histórico familiar.
        builder.HasIndex(t => new
        {
            t.FamilyId,
            t.Scope,
            t.Date
        });

        // Histórico por beneficiário.
        builder.HasIndex(t => new
        {
            t.BeneficiaryUserId,
            t.Date
        });

        // Histórico do plano financeiro.
        builder.HasIndex(t => new
        {
            t.FinancialPlanId,
            t.Date
        });

        // =====================================================
        // PROPRIEDADES COMUNS
        // =====================================================

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired(false);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}