using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        builder.HasKey(ns => ns.Id);

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adı korunuyor
        builder.Property(ns => ns.NotificationTypeId).HasColumnName("NotificationType");

        builder.HasOne(ns => ns.User)
            .WithMany()
            .HasForeignKey(ns => ns.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Her kullanıcı için her bildirim türü tek kayıt
        builder.HasIndex(ns => new { ns.UserId, ns.NotificationTypeId }).IsUnique();
    }
}
