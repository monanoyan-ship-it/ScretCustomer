using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AnswerText)
            .HasMaxLength(2000);

        builder.Property(a => a.EarnedPoints)
            .HasPrecision(10, 2);

        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Veritabanı kolon adlarını korumak için
        builder.Property(a => a.AppliedPenaltyTypeId).HasColumnName("AppliedPenaltyType");
    }
}
