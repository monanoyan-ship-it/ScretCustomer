using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDinlemeAyarConfiguration : IEntityTypeConfiguration<GmDinlemeAyar>
{
    public void Configure(EntityTypeBuilder<GmDinlemeAyar> builder)
    {
        builder.ToTable("GmDinlemeAyarlar");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.GmDonem)
            .WithMany()
            .HasForeignKey(x => x.GmDonemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Checklist)
            .WithMany()
            .HasForeignKey(x => x.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GmDonemId, x.CustomerId }).IsUnique();
    }
}
