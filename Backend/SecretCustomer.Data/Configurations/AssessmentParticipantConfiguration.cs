using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class AssessmentParticipantConfiguration : IEntityTypeConfiguration<AssessmentParticipant>
{
    public void Configure(EntityTypeBuilder<AssessmentParticipant> builder)
    {
        builder.ToTable("AssessmentParticipants");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.AssignmentPeriod)
            .WithMany()
            .HasForeignKey(x => x.AssignmentPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CustomerPersonnel)
            .WithMany()
            .HasForeignKey(x => x.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing: Parent-Child hiyerarşi
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AssignmentPeriodId);
        builder.HasIndex(x => x.CustomerPersonnelId);
        builder.HasIndex(x => x.ParentId);

        // Aynı dönemde aynı kişi birden fazla eklenemez
        builder.HasIndex(x => new { x.AssignmentPeriodId, x.CustomerPersonnelId }).IsUnique();
    }
}
