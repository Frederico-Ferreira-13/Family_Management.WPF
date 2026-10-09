using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.Type)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.FamilyId)
            .IsRequired(false);

        // Money Value Object
        builder.OwnsOne(a => a.Balance, balance =>
        {
            balance.Property(m => m.Amount)
                .HasColumnName("Balance")
                .HasPrecision(18, 2)
                .IsRequired();

            balance.Property(m => m.Currency)
                .HasColumnName("BalanceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasOne<User>()
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relação Account -> Transactions
        //
        // Transaction.AccountId será configurado com mais detalhe
        // em TransactionConfiguration.
        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Coleção gerida pelo campo privado _transactions.
        builder.Navigation(a => a.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // BaseEntity
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired(false);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Evita duas contas ativas/antigas com combinações completamente
        // desnecessárias de pesquisa sem indexação.
        builder.HasIndex(a => a.UserId);

        builder.HasIndex(a => a.FamilyId);

        builder.HasIndex(a => new
        {
            a.UserId,
            a.Name
        });
    }
}