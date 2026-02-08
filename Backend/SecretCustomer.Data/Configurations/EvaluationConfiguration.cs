using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.HasKey(e => e.Id);

        // Veritabanı kolon adını korumak için
        builder.Property(e => e.StatusId).HasColumnName("Status");

        builder.Property(e => e.TotalScore)
            .HasPrecision(10, 2);

        builder.Property(e => e.MaxScore)
            .HasPrecision(10, 2);

        builder.Property(e => e.ScorePercentage)
            .HasPrecision(5, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.HasOne(e => e.Assignment)
            .WithMany(a => a.Evaluations)
            .HasForeignKey(e => e.AssignmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Checklist)
            .WithMany()
            .HasForeignKey(e => e.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Evaluator)
            .WithMany(u => u.Evaluations)
            .HasForeignKey(e => e.EvaluatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Answers)
            .WithOne(a => a.Evaluation)
            .HasForeignKey(a => a.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
