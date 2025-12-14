using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LanguageCulture)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.UniqueSeoCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.FlagImageFileName)
            .HasMaxLength(100);

        // Unique index on UniqueSeoCode
        builder.HasIndex(e => e.UniqueSeoCode)
            .IsUnique();

        // Index for active languages
        builder.HasIndex(e => e.IsActive);

        // Relationship with LocaleStringResource
        builder.HasMany(e => e.LocaleStringResources)
            .WithOne(r => r.Language)
            .HasForeignKey(r => r.LanguageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
