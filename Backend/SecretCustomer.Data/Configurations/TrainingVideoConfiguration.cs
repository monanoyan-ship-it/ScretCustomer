using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class TrainingVideoConfiguration : IEntityTypeConfiguration<TrainingVideo>
{
    public void Configure(EntityTypeBuilder<TrainingVideo> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(2000);

        builder.Property(v => v.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(v => v.ThumbnailPath)
            .HasMaxLength(1000);

        builder.HasIndex(v => v.Title);
        builder.HasIndex(v => v.IsActive);
    }
}

public class TrainingVideoScopeConfiguration : IEntityTypeConfiguration<TrainingVideoScope>
{
    public void Configure(EntityTypeBuilder<TrainingVideoScope> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.QuestionGroupName)
            .HasMaxLength(200);

        builder.HasOne(s => s.TrainingVideo)
            .WithMany(v => v.Scopes)
            .HasForeignKey(s => s.TrainingVideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Checklist)
            .WithMany()
            .HasForeignKey(s => s.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Question)
            .WithMany()
            .HasForeignKey(s => s.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.TrainingVideoId);
        builder.HasIndex(s => s.ScopeTypeId);
        builder.HasIndex(s => s.ChecklistId);
        builder.HasIndex(s => s.QuestionGroupName);
    }
}

public class TrainingVideoAssignmentConfiguration : IEntityTypeConfiguration<TrainingVideoAssignment>
{
    public void Configure(EntityTypeBuilder<TrainingVideoAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(a => a.TrainingVideo)
            .WithMany(v => v.Assignments)
            .HasForeignKey(a => a.TrainingVideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.SourceProject)
            .WithMany()
            .HasForeignKey(a => a.SourceProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.TrainingVideoId);
        builder.HasIndex(a => a.StartDate);
        builder.HasIndex(a => a.DueDate);
        builder.HasIndex(a => a.IsActive);
    }
}

public class TrainingVideoParticipantConfiguration : IEntityTypeConfiguration<TrainingVideoParticipant>
{
    public void Configure(EntityTypeBuilder<TrainingVideoParticipant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Assignment)
            .WithMany(a => a.Participants)
            .HasForeignKey(p => p.TrainingVideoAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CustomerPersonnel)
            .WithMany()
            .HasForeignKey(p => p.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.SourceEvaluation)
            .WithMany()
            .HasForeignKey(p => p.SourceEvaluationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.TrainingVideoAssignmentId);
        builder.HasIndex(p => p.CustomerPersonnelId);
        builder.HasIndex(p => p.StatusId);
        builder.HasIndex(p => new { p.TrainingVideoAssignmentId, p.CustomerPersonnelId }).IsUnique();
    }
}
