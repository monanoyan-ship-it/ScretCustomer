using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class TrainingConfiguration : IEntityTypeConfiguration<Training>
{
    public void Configure(EntityTypeBuilder<Training> builder)
    {
        builder.HasKey(e => e.Id);

        // Veritabanı kolon adlarını korumak için
        builder.Property(e => e.TrainingTypeId).HasColumnName("TrainingType");
        builder.Property(e => e.StatusId).HasColumnName("Status");
        builder.Property(e => e.CategoryId).HasColumnName("Category");
    }
}

public class TrainingParticipantConfiguration : IEntityTypeConfiguration<TrainingParticipant>
{
    public void Configure(EntityTypeBuilder<TrainingParticipant> builder)
    {
        builder.HasKey(e => e.Id);

        // Veritabanı kolon adlarını korumak için
        builder.Property(e => e.StatusId).HasColumnName("Status");
    }
}
