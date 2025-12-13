using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.IsActive);
    }
}
