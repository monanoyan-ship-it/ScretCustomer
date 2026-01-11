using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(e => e.Id);

        // Veritabanı kolon adlarını korumak için
        builder.Property(e => e.MeetingTypeId).HasColumnName("MeetingType");
        builder.Property(e => e.StatusId).HasColumnName("Status");
    }
}

public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.HasKey(e => e.Id);

        // Veritabanı kolon adlarını korumak için
        builder.Property(e => e.StatusId).HasColumnName("Status");
    }
}
