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

        builder.HasOne(x => x.GmSoru)
            .WithMany()
            .HasForeignKey(x => x.GmSoruId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GmDonemId);
        builder.HasIndex(x => x.GmSoruId);
    }
}
