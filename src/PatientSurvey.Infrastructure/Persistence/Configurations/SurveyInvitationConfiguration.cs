using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class SurveyInvitationConfiguration : IEntityTypeConfiguration<SurveyInvitation>
{
    public void Configure(EntityTypeBuilder<SurveyInvitation> builder)
    {
        builder.ToTable("survey_invitations");
        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Id).HasColumnName("id");
        builder.Property(invitation => invitation.SurveyId).HasColumnName("survey_id").IsRequired();
        builder.Property(invitation => invitation.PatientVisitId).HasColumnName("patient_visit_id").IsRequired();
        builder.Property(invitation => invitation.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(invitation => invitation.DeliveryMethod).HasColumnName("delivery_method").HasConversion<int>().IsRequired();
        builder.Property(invitation => invitation.DeliveryStatus).HasColumnName("delivery_status").HasConversion<int>().IsRequired();
        builder.Property(invitation => invitation.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(invitation => invitation.SentAtUtc).HasColumnName("sent_at_utc");

        builder.HasOne(invitation => invitation.Survey)
            .WithMany(survey => survey.Invitations)
            .HasForeignKey(invitation => invitation.SurveyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(invitation => invitation.PatientVisit)
            .WithMany(visit => visit.Invitations)
            .HasForeignKey(invitation => invitation.PatientVisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(invitation => invitation.CreatedByUser)
            .WithMany(user => user.CreatedSurveyInvitations)
            .HasForeignKey(invitation => invitation.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
