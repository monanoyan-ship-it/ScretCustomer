using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class VisitFieldDefinitionConfiguration : IEntityTypeConfiguration<VisitFieldDefinition>
{
    public void Configure(EntityTypeBuilder<VisitFieldDefinition> builder)
    {
        builder.ToTable("VisitFieldDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Placeholder)
            .HasMaxLength(200);

        builder.Property(x => x.HelpText)
            .HasMaxLength(500);

        // Enum conversions
        builder.Property(x => x.FieldType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<int>();

        // Relationship to Sector (nullable - null means common field)
        builder.HasOne(x => x.Sector)
            .WithMany(s => s.FieldDefinitions)
            .HasForeignKey(x => x.SectorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on Code per Sector (or globally for common fields)
        builder.HasIndex(x => new { x.SectorId, x.Code }).IsUnique();

        // Index for sorting
        builder.HasIndex(x => new { x.SectorId, x.Category, x.SortOrder });
    }
}
