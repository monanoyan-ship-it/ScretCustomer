using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(500);

        builder.Property(n => n.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(n => n.GroupId)
            .HasMaxLength(100);

        builder.Property(n => n.AdditionalData)
            .HasColumnType("text");

        // Enum -> TypeId dönüşümünde veri kaybı olmaması için column adları korunuyor
        builder.Property(n => n.NotificationTypeId).HasColumnName("NotificationType");
        builder.Property(n => n.ChannelId).HasColumnName("Channel");
        builder.Property(n => n.PriorityId).HasColumnName("Priority");

        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.SenderUser)
            .WithMany()
            .HasForeignKey(n => n.SenderUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.RecipientUserId);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.NotificationTypeId);
    }
}
