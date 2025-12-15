using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class VisitSectorConfiguration : IEntityTypeConfiguration<VisitSector>
{
    public void Configure(EntityTypeBuilder<VisitSector> builder)
    {
        builder.ToTable("VisitSectors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IconClass)
            .HasMaxLength(100);

        // Unique index on Code
        builder.HasIndex(x => x.Code).IsUnique();

        // Index for sorting
        builder.HasIndex(x => x.SortOrder);
    }
}
