using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class FamilyConfiguration
    : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("Families");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.CreatorUserId)
            .IsRequired();

        builder.Property(f => f.InvitationCode)
            .IsRequired()
            .HasMaxLength(Family.InvitationCodeLength)
            .IsFixedLength();

        // Criador da família.
        //
        // Um utilizador pode criar várias famílias.
        builder.HasOne(f => f.CreatorUser)
            .WithMany(u => u.FamiliesCreated)
            .HasForeignKey(f => f.CreatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Membros da família.
        //
        // Um utilizador pode pertencer a zero ou uma família.
        builder.HasMany(f => f.Members)
            .WithOne(u => u.Family)
            .HasForeignKey(u => u.FamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        // A coleção Members é encapsulada pelo backing field _members.
        builder.Navigation(f => f.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // O código de convite tem de ser globalmente único.
        builder.HasIndex(f => f.InvitationCode)
            .IsUnique();

        builder.HasIndex(f => f.CreatorUserId);

        // Útil para pesquisas de famílias ativas por nome.
        builder.HasIndex(f => f.Name);

        // BaseEntity
        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .IsRequired(false);

        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}