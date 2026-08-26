using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class PatientVisitConfiguration : IEntityTypeConfiguration<PatientVisit>
{
    public void Configure(EntityTypeBuilder<PatientVisit> builder)
    {
        builder.ToTable("patient_visits", table => table.HasCheckConstraint(
            "ck_patient_visits_doctor_department_pair",
            "(doctor_id IS NULL AND department_id IS NULL) OR (doctor_id IS NOT NULL AND department_id IS NOT NULL)"));
        builder.HasKey(visit => visit.Id);

        builder.Property(visit => visit.Id).HasColumnName("id");
        builder.Property(visit => visit.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(visit => visit.DoctorId).HasColumnName("doctor_id");
        builder.Property(visit => visit.DepartmentId).HasColumnName("department_id");
        builder.Property(visit => visit.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(visit => visit.ExaminedAtUtc).HasColumnName("examined_at_utc").IsRequired();
        builder.Property(visit => visit.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(visit => visit.Patient)
            .WithMany(patient => patient.Visits)
            .HasForeignKey(visit => visit.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(visit => visit.Doctor)
            .WithMany(doctor => doctor.PatientVisits)
            .HasForeignKey(visit => visit.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(visit => visit.Department)
            .WithMany(department => department.PatientVisits)
            .HasForeignKey(visit => visit.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(visit => visit.CreatedByUser)
            .WithMany(user => user.CreatedPatientVisits)
            .HasForeignKey(visit => visit.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
