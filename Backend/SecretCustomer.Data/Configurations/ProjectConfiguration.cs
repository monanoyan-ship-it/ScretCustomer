using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired();

        builder.Property(p => p.Description);

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(p => p.AssignmentTypeId).HasColumnName("AssignmentType");
        builder.Property(p => p.StatusId).HasColumnName("Status");
        builder.Property(p => p.ProjectTypeId).HasColumnName("ProjectType");

        builder.HasOne(p => p.Checklist)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.Code); // Unique değil, sadece index
        builder.HasIndex(p => p.StatusId);
    }
}
