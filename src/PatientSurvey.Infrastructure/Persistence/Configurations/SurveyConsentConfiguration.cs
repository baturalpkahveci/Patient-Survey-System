using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class SurveyConsentConfiguration : IEntityTypeConfiguration<SurveyConsent>
{
    public void Configure(EntityTypeBuilder<SurveyConsent> builder)
    {
        builder.ToTable("survey_consents");
        builder.HasKey(consent => consent.Id);
        builder.HasIndex(consent => consent.SurveyInvitationId).IsUnique();

        builder.Property(consent => consent.Id).HasColumnName("id");
        builder.Property(consent => consent.SurveyInvitationId).HasColumnName("survey_invitation_id").IsRequired();
        builder.Property(consent => consent.NoticeVersion).HasColumnName("notice_version").HasMaxLength(20).IsRequired();
        builder.Property(consent => consent.AcceptedAtUtc).HasColumnName("accepted_at_utc").IsRequired();

        builder.HasOne(consent => consent.SurveyInvitation)
            .WithOne(invitation => invitation.Consent)
            .HasForeignKey<SurveyConsent>(consent => consent.SurveyInvitationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
