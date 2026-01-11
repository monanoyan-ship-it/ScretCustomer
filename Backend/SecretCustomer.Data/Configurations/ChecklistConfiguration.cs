using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(c => c.ChecklistTypeId).HasColumnName("ChecklistType");
        builder.Property(c => c.ScoringMethodId).HasColumnName("ScoringMethod");
    }
}
