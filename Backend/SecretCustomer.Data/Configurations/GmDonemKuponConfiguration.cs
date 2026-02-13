using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDonemKuponConfiguration : IEntityTypeConfiguration<GmDonemKupon>
{
    public void Configure(EntityTypeBuilder<GmDonemKupon> builder)
    {
        builder.ToTable("GmDonemKuponlar");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.KuponKodu)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.GmDonemId);
    }
}
