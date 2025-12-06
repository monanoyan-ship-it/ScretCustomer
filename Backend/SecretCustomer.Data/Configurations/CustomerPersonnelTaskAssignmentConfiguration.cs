using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class CustomerPersonnelTaskAssignmentConfiguration : IEntityTypeConfiguration<CustomerPersonnelTaskAssignment>
{
    public void Configure(EntityTypeBuilder<CustomerPersonnelTaskAssignment> builder)
    {
        builder.ToTable("CustomerPersonnelTaskAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(a => new { a.PersonnelId, a.TaskListId });
    }
}