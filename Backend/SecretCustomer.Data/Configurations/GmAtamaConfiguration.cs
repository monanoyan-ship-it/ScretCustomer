using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmAtamaConfiguration : IEntityTypeConfiguration<GmAtama>
{
    public void Configure(EntityTypeBuilder<GmAtama> builder)
    {
        builder.ToTable("GmAtamalar");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AramaSaati)
            .HasMaxLength(10);

        builder.Property(x => x.Not)
            .HasMaxLength(2000);

        builder.Property(x => x.KuponKodu)
            .HasMaxLength(100);

        builder.Property(x => x.KuponBilgisi)
            .HasMaxLength(500);

        builder.HasOne(x => x.GmDonemSoru)
            .WithMany()
            .HasForeignKey(x => x.GmDonemSoruId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.GmDonemId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DurumId);
    }
}
