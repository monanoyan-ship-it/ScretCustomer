using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDonemPersonelConfiguration : IEntityTypeConfiguration<GmDonemPersonel>
{
    public void Configure(EntityTypeBuilder<GmDonemPersonel> builder)
    {
        builder.ToTable("GmDonemPersoneller");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.GmDonemId);
        builder.HasIndex(x => x.UserId);
    }
}
