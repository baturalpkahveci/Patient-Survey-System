using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("survey_responses");
        builder.HasKey(response => response.Id);
        builder.HasIndex(response => response.TokenId).IsUnique();

        builder.Property(response => response.Id).HasColumnName("id");
        builder.Property(response => response.TokenId).HasColumnName("token_id");
        builder.Property(response => response.DepartmentId).HasColumnName("department_id");
        builder.Property(response => response.SubmittedAtUtc).HasColumnName("submitted_at_utc").IsRequired();

        builder.HasOne(response => response.Token)
            .WithOne(token => token.SurveyResponse)
            .HasForeignKey<SurveyResponse>(response => response.TokenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(response => response.Department)
            .WithMany(department => department.SurveyResponses)
            .HasForeignKey(response => response.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
