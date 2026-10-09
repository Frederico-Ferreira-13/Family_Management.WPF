using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class InvestmentConfiguration
    : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("Investments");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.Type)
            .IsRequired();

        // InitialValue : Money
        builder.OwnsOne(i => i.InitialValue, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("InitialValue")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("InitialCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // CurrentValue : Money
        builder.OwnsOne(i => i.CurrentValue, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CurrentValue")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("CurrentCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(i => i.PurchaseDate)
            .IsRequired();

        builder.Property(i => i.LastUpdateDate)
            .IsRequired(false);

        builder.Property(i => i.UserId)
            .IsRequired();

        builder.Property(i => i.AccountId)
            .IsRequired(false);

        // Investment -> User
        builder.HasOne(i => i.User)
            .WithMany(u => u.Investments)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Investment -> Account
        //
        // Account já não contém ICollection<Investment>.
        builder.HasOne(i => i.Account)
            .WithMany()
            .HasForeignKey(i => i.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        // Propriedades calculadas
        builder.Ignore(i => i.ProfitLoss);
        builder.Ignore(i => i.ProfitLossPercentage);
        builder.Ignore(i => i.IsProfitable);

        // Índices
        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.AccountId);
        builder.HasIndex(i => i.PurchaseDate);

        // BaseEntity
        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired(false);

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}