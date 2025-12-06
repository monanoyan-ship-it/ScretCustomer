using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class CustomerPersonnelPermissionConfiguration : IEntityTypeConfiguration<CustomerPersonnelPermission>
{
    public void Configure(EntityTypeBuilder<CustomerPersonnelPermission> builder)
    {
        builder.ToTable("CustomerPersonnelPermissions");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.PersonnelId, p.PermissionType });

        builder.Property(p => p.Description)
            .HasMaxLength(500);
    }
}