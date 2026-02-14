using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmHedefFirmaConfiguration : IEntityTypeConfiguration<GmHedefFirma>
{
    public void Configure(EntityTypeBuilder<GmHedefFirma> builder)
    {
        builder.ToTable("GmHedefFirmalar");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirmaAdi)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.TelefonNo)
            .HasMaxLength(50);

        builder.Property(x => x.Aciklama)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CustomerId);
    }
}
