using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class VisitDetailValueConfiguration : IEntityTypeConfiguration<VisitDetailValue>
{
    public void Configure(EntityTypeBuilder<VisitDetailValue> builder)
    {
        builder.ToTable("VisitDetailValues");

        builder.HasKey(x => x.Id);

        // Typed value columns
        builder.Property(x => x.IntValue);
        builder.Property(x => x.DecimalValue)
            .HasPrecision(18, 4);
        builder.Property(x => x.BoolValue);
        builder.Property(x => x.StringValue)
            .HasMaxLength(4000);
        builder.Property(x => x.DateTimeValue);

        // Relationship to CustomerVisit
        builder.HasOne(x => x.CustomerVisit)
            .WithMany(c => c.DetailValues)
            .HasForeignKey(x => x.CustomerVisitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to FieldDefinition
        builder.HasOne(x => x.FieldDefinition)
            .WithMany(f => f.DetailValues)
            .HasForeignKey(x => x.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for queryability
        builder.HasIndex(x => x.CustomerVisitId);
        builder.HasIndex(x => x.FieldDefinitionId);
        builder.HasIndex(x => new { x.CustomerVisitId, x.FieldDefinitionId }).IsUnique();
        builder.HasIndex(x => new { x.FieldDefinitionId, x.IntValue });
        builder.HasIndex(x => new { x.FieldDefinitionId, x.BoolValue });
    }
}
