
using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration
    : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Month)
            .IsRequired();

        builder.Property(b => b.Year)
            .IsRequired();

        builder.Property(b => b.CategoryId)
            .IsRequired();

        // Utilizador que criou o orçamento.
        builder.Property(b => b.UserId)
            .IsRequired();

        // Âmbito familiar opcional.
        builder.Property(b => b.FamilyId)
            .IsRequired(false);

        // Value Object: BudgetedAmount
        builder.OwnsOne(b => b.BudgetedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("BudgetedAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("BudgetedCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Value Object: CurrentSpent
        builder.OwnsOne(b => b.CurrentSpent, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CurrentSpent")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("CurrentSpentCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Budget -> User
        //
        // UserId identifica o criador do orçamento.
        // A coleção User.Budgets corresponde a esta relação.
        builder.HasOne<User>()
            .WithMany(u => u.Budgets)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Budget -> Family
        //
        // Uma família pode possuir vários orçamentos.
        // Impedimos a eliminação física da família enquanto
        // existirem orçamentos associados.
        builder.HasOne(b => b.Family)
            .WithMany()
            .HasForeignKey(b => b.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Budget -> Category
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Orçamentos pessoais:
        // um orçamento por utilizador, categoria e período.
        builder.HasIndex(b => new
        {
            b.UserId,
            b.CategoryId,
            b.Month,
            b.Year
        })
        .IsUnique()
        .HasDatabaseName("UX_Budgets_Personal_Period")
        .HasFilter("\"FamilyId\" IS NULL");

        // Orçamentos familiares:
        // um orçamento por família, categoria e período,
        // independentemente do membro que o criou.
        builder.HasIndex(b => new
        {
            b.FamilyId,
            b.CategoryId,
            b.Month,
            b.Year
        })
        .IsUnique()
        .HasDatabaseName("UX_Budgets_Family_Period")
        .HasFilter("\"FamilyId\" IS NOT NULL");

        // Consultas pessoais por período.
        builder.HasIndex(b => new
        {
            b.UserId,
            b.Year,
            b.Month
        });

        // Consultas familiares por período.
        builder.HasIndex(b => new
        {
            b.FamilyId,
            b.Year,
            b.Month
        });

        // Propriedades calculadas exclusivamente no domínio.
        builder.Ignore(b => b.RemainingAmount);
        builder.Ignore(b => b.IsOverBudget);
        builder.Ignore(b => b.IsFamilyBudget);

        // BaseEntity
        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired(false);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}