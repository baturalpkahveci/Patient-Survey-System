using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Application.Security;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(permission => permission.Id);
        builder.HasIndex(permission => permission.Name).IsUnique();

        builder.Property(permission => permission.Id).HasColumnName("id");
        builder.Property(permission => permission.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(permission => permission.Description).HasColumnName("description").HasMaxLength(300);
        builder.Property(permission => permission.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasData(new Permission
        {
            Id = 1,
            Name = AppPermissions.CanViewPatientPersonalData,
            Description = "Hasta kişisel verilerini görüntüleyebilir.",
            IsActive = true
        });
    }
}
