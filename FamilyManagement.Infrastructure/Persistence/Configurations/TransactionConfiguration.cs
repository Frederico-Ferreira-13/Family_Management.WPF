
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

        // Valor monetário
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

        // Utilizador responsável pelo movimento
        builder.Property(t => t.UserId)
            .IsRequired();

        // Âmbito financeiro:
        // null = movimento pessoal
        // valor = movimento familiar
        builder.Property(t => t.FamilyId)
            .IsRequired(false);

        // Conta bancária opcional para receitas e despesas.
        // As transferências continuam a exigir contas
        // de origem e destino, por regra de domínio.
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

        // Plano financeiro opcional para compatibilidade
        // com os movimentos anteriores à sua introdução.
        builder.Property(t => t.FinancialPlanId)
            .IsRequired(false);

        // Utilizador -> Movimentos
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Família -> Movimentos
        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plano financeiro -> Movimentos
        builder.HasOne(t => t.FinancialPlan)
            .WithMany()
            .HasForeignKey(t => t.FinancialPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Conta bancária -> Movimentos
        // A relação é opcional, mas impede a eliminação
        // física de uma conta com histórico associado.
        builder.HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Conta de destino das transferências
        builder.HasOne(t => t.TargetAccount)
            .WithMany()
            .HasForeignKey(t => t.TargetAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categoria -> Movimentos
        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Orçamento -> Movimentos
        builder.HasOne(t => t.Budget)
            .WithMany()
            .HasForeignKey(t => t.BudgetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Objetivo -> Contribuições
        builder.HasOne(t => t.Goal)
            .WithMany(g => g.Contributions)
            .HasForeignKey(t => t.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        // Recorrência -> Movimentos gerados
        builder.HasOne(t => t.RecurringTransaction)
            .WithMany(rt => rt.GeneratedTransactions)
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índices de pesquisa
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.BudgetId);
        builder.HasIndex(t => t.GoalId);
        builder.HasIndex(t => t.RecurringTransactionId);
        builder.HasIndex(t => t.Type);

        // Extrato bancário
        builder.HasIndex(t => new
        {
            t.AccountId,
            t.Date
        });

        // Histórico pessoal
        builder.HasIndex(t => new
        {
            t.UserId,
            t.Date
        });

        // Histórico familiar
        builder.HasIndex(t => new
        {
            t.FamilyId,
            t.Date
        });

        // Histórico do plano financeiro
        builder.HasIndex(t => new
        {
            t.FinancialPlanId,
            t.Date
        });

        // Propriedades comuns
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired(false);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
