using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class ProjectFileConfiguration : IEntityTypeConfiguration<ProjectFile>
{
    public void Configure(EntityTypeBuilder<ProjectFile> builder)
    {
        builder.HasKey(pf => pf.Id);

        builder.Property(pf => pf.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pf => pf.OriginalFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pf => pf.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(pf => pf.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pf => pf.Description)
            .HasMaxLength(500);

        builder.HasOne(pf => pf.Project)
            .WithMany(p => p.Files)
            .HasForeignKey(pf => pf.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pf => pf.UploadedBy)
            .WithMany()
            .HasForeignKey(pf => pf.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(pf => pf.ProjectId);
    }
}
