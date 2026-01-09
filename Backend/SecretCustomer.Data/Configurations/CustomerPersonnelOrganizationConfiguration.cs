using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class CustomerPersonnelOrganizationConfiguration : IEntityTypeConfiguration<CustomerPersonnelOrganization>
{
    public void Configure(EntityTypeBuilder<CustomerPersonnelOrganization> builder)
    {
        builder.HasKey(po => po.Id);

        builder.Property(po => po.Notes)
            .HasMaxLength(500);

        // CustomerPersonnel iliskisi
        builder.HasOne(po => po.CustomerPersonnel)
            .WithMany(p => p.OrganizationAssignments)
            .HasForeignKey(po => po.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        // CustomerOrganization iliskisi
        builder.HasOne(po => po.CustomerOrganization)
            .WithMany(o => o.PersonnelAssignments)
            .HasForeignKey(po => po.CustomerOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Supervisor iliskisi (self-referencing through CustomerPersonnel)
        builder.HasOne(po => po.Supervisor)
            .WithMany()
            .HasForeignKey(po => po.SupervisorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Composite unique index: Her personel-organizasyon kombinasyonu icin tek kayit
        builder.HasIndex(po => new { po.CustomerPersonnelId, po.CustomerOrganizationId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
