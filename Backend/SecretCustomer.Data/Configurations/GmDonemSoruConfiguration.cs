using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDonemSoruConfiguration : IEntityTypeConfiguration<GmDonemSoru>
{
    public void Configure(EntityTypeBuilder<GmDonemSoru> builder)
    {
        builder.ToTable("GmDonemSorular");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SoruMetni)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.BeklenenCevap)
            .HasMaxLength(2000);

        builder.Property(x => x.KuponBilgisi)
            .HasMaxLength(500);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.GmHedefFirma)
            .WithMany(h => h.DonemSorular)
            .HasForeignKey(x => x.GmHedefFirmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GmSoru)
            .WithMany()
            .HasForeignKey(x => x.GmSoruId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GmDonemId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.GmHedefFirmaId);
    }
}
