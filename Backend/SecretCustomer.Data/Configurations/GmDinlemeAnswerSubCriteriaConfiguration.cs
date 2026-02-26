using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class GmDinlemeAnswerSubCriteriaConfiguration : IEntityTypeConfiguration<GmDinlemeAnswerSubCriteria>
{
    public void Configure(EntityTypeBuilder<GmDinlemeAnswerSubCriteria> builder)
    {
        builder.ToTable("GmDinlemeAnswerSubCriteria");
        builder.HasKey(x => new { x.GmDinlemeAnswerId, x.SubCriteriaId });

        builder.HasOne(x => x.GmDinlemeAnswer)
            .WithMany(a => a.SubCriteriaSelections)
            .HasForeignKey(x => x.GmDinlemeAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SubCriteria)
            .WithMany()
            .HasForeignKey(x => x.SubCriteriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
