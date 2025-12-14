using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class LocaleStringResourceConfiguration : IEntityTypeConfiguration<LocaleStringResource>
{
    public void Configure(EntityTypeBuilder<LocaleStringResource> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ResourceName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ResourceValue)
            .IsRequired();

        // Composite unique index on LanguageId + ResourceName
        builder.HasIndex(e => new { e.LanguageId, e.ResourceName })
            .IsUnique();

        // Index for searching by resource name
        builder.HasIndex(e => e.ResourceName);
    }
}
