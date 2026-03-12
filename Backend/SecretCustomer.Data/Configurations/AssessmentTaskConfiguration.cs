using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Data.Configurations;

public class AssessmentTaskConfiguration : IEntityTypeConfiguration<AssessmentTask>
{
    public void Configure(EntityTypeBuilder<AssessmentTask> builder)
    {
        builder.ToTable("AssessmentTasks");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SurveyInvitation)
            .WithMany()
            .HasForeignKey(x => x.SurveyInvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EvaluatedCustomerPersonnel)
            .WithMany()
            .HasForeignKey(x => x.EvaluatedCustomerPersonnelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Evaluation)
            .WithMany()
            .HasForeignKey(x => x.EvaluationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.SurveyInvitationId);
        builder.HasIndex(x => x.EvaluatedCustomerPersonnelId);
        builder.HasIndex(x => x.EvaluationId);
    }
}
