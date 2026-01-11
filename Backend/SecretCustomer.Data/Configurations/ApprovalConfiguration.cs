using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ReferenceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Title)
            .IsRequired();

        builder.Property(a => a.Description);

        builder.Property(a => a.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(a => a.ResponseNote)
            .HasMaxLength(2000);

        builder.Property(a => a.AdditionalData)
            .HasColumnType("text");

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adları korunuyor
        builder.Property(a => a.ApprovalTypeId).HasColumnName("ApprovalType");
        builder.Property(a => a.StatusId).HasColumnName("Status");
        builder.Property(a => a.PriorityId).HasColumnName("Priority");

        builder.HasOne(a => a.RequestedByUser)
            .WithMany()
            .HasForeignKey(a => a.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApproverUser)
            .WithMany()
            .HasForeignKey(a => a.ApproverUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ApprovedByUser)
            .WithMany()
            .HasForeignKey(a => a.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.ReferenceNumber).IsUnique();
        builder.HasIndex(a => a.StatusId);
        builder.HasIndex(a => a.ApprovalTypeId);
    }
}
