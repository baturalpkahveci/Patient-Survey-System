using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(role => role.Id);
        builder.HasIndex(role => role.Name).IsUnique();

        builder.Property(role => role.Id).HasColumnName("id");
        builder.Property(role => role.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(role => role.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasData(
            new Role { Id = 1, Name = "Admin", IsActive = true },
            new Role { Id = 2, Name = "Manager", IsActive = true });
    }
}
