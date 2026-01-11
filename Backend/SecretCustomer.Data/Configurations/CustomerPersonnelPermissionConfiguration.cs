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

        builder.HasIndex(p => new { p.PersonnelId, p.PermissionTypeId });

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(p => p.PermissionTypeId).HasColumnName("PermissionType");

        // Scope -> ScopeId donusumunde veri kaybi olmamasi icin column adi korunuyor
        builder.Property(p => p.ScopeId).HasColumnName("Scope");
    }
}