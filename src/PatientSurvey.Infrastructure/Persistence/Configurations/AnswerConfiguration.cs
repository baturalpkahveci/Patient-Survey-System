using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers", table => table.HasCheckConstraint(
            "ck_answers_score_value_range",
            "score_value IS NULL OR (score_value >= 1 AND score_value <= 5)"));

        builder.HasKey(answer => answer.Id);
        builder.HasIndex(answer => new { answer.SurveyResponseId, answer.QuestionId }).IsUnique();

        builder.Property(answer => answer.Id).HasColumnName("id");
        builder.Property(answer => answer.SurveyResponseId).HasColumnName("response_id");
        builder.Property(answer => answer.QuestionId).HasColumnName("question_id");
        builder.Property(answer => answer.ScoreValue).HasColumnName("score_value");
        builder.Property(answer => answer.TextValue).HasColumnName("text_value").HasMaxLength(4000);
        builder.Property(answer => answer.BooleanValue).HasColumnName("boolean_value");

        builder.HasOne(answer => answer.SurveyResponse)
            .WithMany(response => response.Answers)
            .HasForeignKey(answer => answer.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(answer => answer.Question)
            .WithMany(question => question.Answers)
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
