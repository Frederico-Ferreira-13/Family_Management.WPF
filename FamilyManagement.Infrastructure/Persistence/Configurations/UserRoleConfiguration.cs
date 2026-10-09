using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration
    : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(200);

        // O nome do role identifica-o funcionalmente.
        builder.HasIndex(r => r.Name)
            .IsUnique();

        // User <-> UserRole (N:N)
        builder.HasMany(r => r.Users)
            .WithMany(u => u.Roles)
            .UsingEntity<Dictionary<string, object>>(
                "UserRoleAssignments",
                right => right
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<UserRole>()
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("UserRoleAssignments");

                    join.HasKey("UserId", "RoleId");

                    join.HasIndex("RoleId");
                });

        builder.Navigation(r => r.Users)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Propriedade calculada.
        builder.Ignore(r => r.IsBuiltIn);

        // BaseEntity
        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        ConfigureSeedData(builder);
    }

    private static void ConfigureSeedData(
        EntityTypeBuilder<UserRole> builder)
    {
        builder.HasData(
            new
            {
                Id = new Guid(
                    "A1111111-1111-1111-1111-111111111111"),
                Name = UserRole.AdminName,
                Description = "Administrador da família.",
                IsActive = true,
                CreatedAt = new DateTime(
                    2025, 6, 16,
                    0, 0, 0,
                    DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = new Guid(
                    "B2222222-2222-2222-2222-222222222222"),
                Name = UserRole.MemberName,
                Description = "Membro da família.",
                IsActive = true,
                CreatedAt = new DateTime(
                    2025, 6, 16,
                    0, 0, 0,
                    DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = new Guid(
                    "C3333333-3333-3333-3333-333333333333"),
                Name = UserRole.ViewerName,
                Description = "Utilizador com acesso de consulta.",
                IsActive = true,
                CreatedAt = new DateTime(
                    2025, 6, 16,
                    0, 0, 0,
                    DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null
            });
    }
}