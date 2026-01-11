using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Notes)
            .HasMaxLength(500);

        // Role -> RoleId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(rp => rp.RoleId).HasColumnName("Role");

        // Scope -> ScopeId donusumunde veri kaybi olmamasi icin column adi korunuyor
        builder.Property(rp => rp.ScopeId).HasColumnName("Scope");

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index: Her rol-permission kombinasyonu için tek kayıt
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
    }
}
