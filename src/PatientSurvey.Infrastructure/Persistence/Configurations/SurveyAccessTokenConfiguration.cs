using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class SurveyAccessTokenConfiguration : IEntityTypeConfiguration<SurveyAccessToken>
{
    public void Configure(EntityTypeBuilder<SurveyAccessToken> builder)
    {
        builder.ToTable("survey_access_tokens");
        builder.HasKey(token => token.Id);
        builder.HasIndex(token => token.Token).IsUnique();
        builder.HasIndex(token => token.SurveyInvitationId).IsUnique();

        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.SurveyId).HasColumnName("survey_id");
        builder.Property(token => token.SurveyInvitationId).HasColumnName("survey_invitation_id");
        builder.Property(token => token.Token).HasColumnName("token").HasMaxLength(128).IsRequired();
        builder.Property(token => token.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(token => token.UsedAtUtc).HasColumnName("used_at");

        builder.HasOne(token => token.Survey)
            .WithMany(survey => survey.AccessTokens)
            .HasForeignKey(token => token.SurveyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(token => token.SurveyInvitation)
            .WithOne(invitation => invitation.AccessToken)
            .HasForeignKey<SurveyAccessToken>(token => token.SurveyInvitationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
