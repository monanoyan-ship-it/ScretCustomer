using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDinlemeEvaluationConfiguration : IEntityTypeConfiguration<GmDinlemeEvaluation>
{
    public void Configure(EntityTypeBuilder<GmDinlemeEvaluation> builder)
    {
        builder.ToTable("GmDinlemeEvaluations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalScore).HasPrecision(18, 2);
        builder.Property(x => x.MaxScore).HasPrecision(18, 2);
        builder.Property(x => x.Percentage).HasPrecision(18, 2);
        builder.Property(x => x.Comment).HasMaxLength(4000);

        builder.HasOne(x => x.GmAtama)
            .WithMany()
            .HasForeignKey(x => x.GmAtamaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Checklist)
            .WithMany()
            .HasForeignKey(x => x.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DinleyenUser)
            .WithMany()
            .HasForeignKey(x => x.DinleyenUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GmAtamaId);
        builder.HasIndex(x => x.DinleyenUserId);
        builder.HasIndex(x => x.DurumId);
    }
}
