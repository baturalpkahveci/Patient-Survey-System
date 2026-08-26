using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(patient => patient.Id);
        builder.HasIndex(patient => patient.TcIdentityLookupHash).IsUnique();

        builder.Property(patient => patient.Id).HasColumnName("id");
        builder.Property(patient => patient.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(patient => patient.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(patient => patient.TcIdentityLookupHash).HasColumnName("tc_identity_lookup_hash").HasMaxLength(128).IsRequired();
        builder.Property(patient => patient.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
        builder.Property(patient => patient.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(patient => patient.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(patient => patient.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
    }
}
