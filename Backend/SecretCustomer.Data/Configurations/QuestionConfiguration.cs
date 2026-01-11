using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(q => q.RecommendedNote)
            .HasMaxLength(2000);

        builder.Property(q => q.HelpText)
            .HasMaxLength(1000);

        // Checklist ilişkisi
        builder.HasOne(q => q.Checklist)
            .WithMany(c => c.Questions)
            .HasForeignKey(q => q.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Veritabanı kolon adlarını korumak için
        builder.Property(q => q.PenaltyTypeId).HasColumnName("PenaltyType");
        builder.Property(q => q.ScoringTypeId).HasColumnName("ScoringType");
    }
}
