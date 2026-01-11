using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class CustomerPersonnelConfiguration : IEntityTypeConfiguration<CustomerPersonnel>
{
    public void Configure(EntityTypeBuilder<CustomerPersonnel> builder)
    {
        builder.ToTable("CustomerPersonnel");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Username)
            .IsRequired()
            .HasMaxLength(100);

        // Username firma bazlı unique (aynı firmada aynı username olamaz)
        builder.HasIndex(p => new { p.CustomerId, p.Username })
            .IsUnique();

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(255);

        // Email firma bazlı unique (aynı firmada aynı email olamaz, farklı firmalarda olabilir)
        builder.HasIndex(p => new { p.CustomerId, p.Email })
            .IsUnique();

        builder.Property(p => p.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(p => p.Department)
            .HasMaxLength(100);

        builder.Property(p => p.Title)
            .HasMaxLength(100);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(p => p.RoleId).HasColumnName("Role");

        // Relationships
        builder.HasMany(p => p.TaskAssignments)
            .WithOne(t => t.Personnel)
            .HasForeignKey(t => t.PersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Permissions)
            .WithOne(p => p.Personnel)
            .HasForeignKey(p => p.PersonnelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}