using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
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

        builder.Property(b => b.UserId)
            .IsRequired();

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
        // Budget não expõe uma navigation User, mas User expõe
        // a coleção Budgets. É importante indicar essa coleção
        // explicitamente para evitar que o EF Core crie uma
        // segunda relação e uma shadow FK como UserId1.
        builder.HasOne<User>()
            .WithMany(u => u.Budgets)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Budget -> Category
        //
        // Budget não expõe navigation Category.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Um utilizador só pode ter um orçamento para a mesma
        // categoria no mesmo mês/ano.
        builder.HasIndex(b => new
        {
            b.UserId,
            b.CategoryId,
            b.Month,
            b.Year
        })
        .IsUnique();

        builder.HasIndex(b => new
        {
            b.UserId,
            b.Year,
            b.Month
        });

        // Propriedades calculadas exclusivamente no domínio.
        builder.Ignore(b => b.RemainingAmount);
        builder.Ignore(b => b.IsOverBudget);

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