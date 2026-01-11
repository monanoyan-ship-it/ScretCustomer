using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Content)
            .IsRequired();

        builder.Property(a => a.Summary)
            .HasMaxLength(500);

        builder.Property(a => a.TargetRoles)
            .HasMaxLength(200);

        // Type -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(a => a.TypeId).HasColumnName("Type");

        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
