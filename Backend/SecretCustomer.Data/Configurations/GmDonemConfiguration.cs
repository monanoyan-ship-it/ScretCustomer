using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDonemConfiguration : IEntityTypeConfiguration<GmDonem>
{
    public void Configure(EntityTypeBuilder<GmDonem> builder)
    {
        builder.ToTable("GmDonemler");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Ad)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OlusturanUser)
            .WithMany()
            .HasForeignKey(x => x.OlusturanUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Kuponlar)
            .WithOne(k => k.GmDonem)
            .HasForeignKey(k => k.GmDonemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Personeller)
            .WithOne(p => p.GmDonem)
            .HasForeignKey(p => p.GmDonemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Sorular)
            .WithOne(s => s.GmDonem)
            .HasForeignKey(s => s.GmDonemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Atamalar)
            .WithOne(a => a.GmDonem)
            .HasForeignKey(a => a.GmDonemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.DurumId);
    }
}
