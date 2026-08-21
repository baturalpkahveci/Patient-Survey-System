using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(question => question.Id);
        builder.HasIndex(question => new { question.SurveyId, question.DisplayOrder });

        builder.Property(question => question.Id).HasColumnName("id");
        builder.Property(question => question.SurveyId).HasColumnName("survey_id");
        builder.Property(question => question.Text).HasColumnName("text").HasMaxLength(1000).IsRequired();
        builder.Property(question => question.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(question => question.IsRequired).HasColumnName("is_required").IsRequired();
        builder.Property(question => question.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(question => question.DisplayOrder).HasColumnName("display_order").IsRequired();

        builder.HasOne(question => question.Survey)
            .WithMany(survey => survey.Questions)
            .HasForeignKey(question => question.SurveyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
