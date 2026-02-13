using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmSoruConfiguration : IEntityTypeConfiguration<GmSoru>
{
    public void Configure(EntityTypeBuilder<GmSoru> builder)
    {
        builder.ToTable("GmSorular");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SoruMetni)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.BeklenenCevap)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.GmHedefFirmaId);
    }
}
