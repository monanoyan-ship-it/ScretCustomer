using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UniqueLink)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(a => a.UniqueLink).IsUnique();

        builder.Property(a => a.ExternalEmail)
            .HasMaxLength(255);

        builder.Property(a => a.ExternalName)
            .HasMaxLength(200);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Assignments)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Checklist)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedUser)
            .WithMany(u => u.Assignments)
            .HasForeignKey(a => a.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.AssignedCustomerPersonnel)
            .WithMany()
            .HasForeignKey(a => a.AssignedCustomerPersonnelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
