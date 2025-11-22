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

        builder.Property(q => q.OptionsJson)
            .HasColumnType("jsonb"); // PostgreSQL JSON tipi

        builder.HasOne(q => q.Section)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
