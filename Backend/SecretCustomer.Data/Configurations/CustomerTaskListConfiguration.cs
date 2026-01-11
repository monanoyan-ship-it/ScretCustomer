using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class CustomerTaskListConfiguration : IEntityTypeConfiguration<CustomerTaskList>
{
    public void Configure(EntityTypeBuilder<CustomerTaskList> builder)
    {
        builder.ToTable("CustomerTaskLists");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(t => t.TaskTypeId).HasColumnName("TaskType");
        builder.Property(t => t.PriorityId).HasColumnName("Priority");
        builder.Property(t => t.StatusId).HasColumnName("Status");

        // Relationships
        builder.HasMany(t => t.PersonnelAssignments)
            .WithOne(a => a.TaskList)
            .HasForeignKey(a => a.TaskListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Checklist)
            .WithMany()
            .HasForeignKey(t => t.ChecklistId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}