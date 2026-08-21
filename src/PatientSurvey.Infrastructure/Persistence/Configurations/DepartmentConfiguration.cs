using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(department => department.Id);
        builder.HasIndex(department => department.Name).IsUnique();

        builder.Property(department => department.Id).HasColumnName("id");
        builder.Property(department => department.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(department => department.IsActive).HasColumnName("is_active").IsRequired();
    }
}
