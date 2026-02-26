using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDinlemeAnswerConfiguration : IEntityTypeConfiguration<GmDinlemeAnswer>
{
    public void Configure(EntityTypeBuilder<GmDinlemeAnswer> builder)
    {
        builder.ToTable("GmDinlemeAnswers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnswerText).HasMaxLength(4000);
        builder.Property(x => x.GivenPoints).HasPrecision(18, 2);
        builder.Property(x => x.EarnedPoints).HasPrecision(18, 2);

        builder.HasOne(x => x.GmDinlemeEvaluation)
            .WithMany(e => e.Answers)
            .HasForeignKey(x => x.GmDinlemeEvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GmDinlemeEvaluationId);
    }
}
