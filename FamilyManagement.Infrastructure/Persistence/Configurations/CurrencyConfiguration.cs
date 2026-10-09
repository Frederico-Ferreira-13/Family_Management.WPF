using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class CurrencyConfiguration
    : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.DecimalPlaces)
            .IsRequired()
            .HasDefaultValue((byte)2);

        builder.Property(c => c.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // O código ISO identifica inequivocamente a moeda.
        builder.HasIndex(c => c.Code)
            .IsUnique();

        // Pesquisa/listagem por nome.
        // Não o torno UNIQUE porque Code é a identidade funcional
        // realmente importante da moeda.
        builder.HasIndex(c => c.Name);

        // BaseEntity
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        ConfigureSeedData(builder);
    }

    private static void ConfigureSeedData(
        EntityTypeBuilder<Currency> builder)
    {
        builder.HasData(
            new
            {
                Id = new Guid(
                    "96c42966-13a9-460d-88f5-450f61202e86"),

                Code = "EUR",
                Name = "Euro",
                Symbol = "€",
                DecimalPlaces = (byte)2,
                IsDefault = true,
                IsActive = true,

                CreatedAt = new DateTime(
                    2025,
                    6,
                    16,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),

                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = new Guid(
                    "67677461-12c4-4b45-9788-51f71a76f2d2"),

                Code = "USD",
                Name = "US Dollar",
                Symbol = "$",
                DecimalPlaces = (byte)2,
                IsDefault = false,
                IsActive = true,

                CreatedAt = new DateTime(
                    2025,
                    6,
                    16,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),

                UpdatedAt = (DateTime?)null
            });
    }
}

