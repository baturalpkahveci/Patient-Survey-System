using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("surveys", table => table.HasCheckConstraint(
            "ck_surveys_doctor_department_pair",
            "(doctor_id IS NULL AND department_id IS NULL) OR (doctor_id IS NOT NULL AND department_id IS NOT NULL)"));
        builder.HasKey(survey => survey.Id);
        builder.Property(survey => survey.Id).HasColumnName("id");
        builder.Property(survey => survey.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(survey => survey.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(survey => survey.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(survey => survey.DoctorId).HasColumnName("doctor_id");
        builder.Property(survey => survey.DepartmentId).HasColumnName("department_id");
        builder.Property(survey => survey.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(survey => survey.Doctor)
            .WithMany(doctor => doctor.Surveys)
            .HasForeignKey(survey => survey.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(survey => survey.Department)
            .WithMany(department => department.Surveys)
            .HasForeignKey(survey => survey.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
