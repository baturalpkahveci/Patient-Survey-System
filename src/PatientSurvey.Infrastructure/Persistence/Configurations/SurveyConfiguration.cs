using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("surveys");
        builder.HasKey(survey => survey.Id);
        builder.Property(survey => survey.Id).HasColumnName("id");
        builder.Property(survey => survey.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(survey => survey.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(survey => survey.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(survey => survey.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}
