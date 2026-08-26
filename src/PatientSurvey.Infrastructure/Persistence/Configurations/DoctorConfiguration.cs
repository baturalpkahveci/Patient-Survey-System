using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");
        builder.HasKey(doctor => doctor.Id);
        builder.HasIndex(doctor => doctor.UserId).IsUnique();

        builder.Property(doctor => doctor.Id).HasColumnName("id");
        builder.Property(doctor => doctor.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(doctor => doctor.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(doctor => doctor.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(doctor => doctor.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(doctor => doctor.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasOne(doctor => doctor.User)
            .WithOne(user => user.Doctor)
            .HasForeignKey<Doctor>(doctor => doctor.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(doctor => doctor.Department)
            .WithMany(department => department.Doctors)
            .HasForeignKey(doctor => doctor.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
