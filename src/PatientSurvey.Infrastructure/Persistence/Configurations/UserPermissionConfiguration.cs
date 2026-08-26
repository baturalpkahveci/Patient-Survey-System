using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");
        builder.HasKey(userPermission => new { userPermission.UserId, userPermission.PermissionId });

        builder.Property(userPermission => userPermission.UserId).HasColumnName("user_id");
        builder.Property(userPermission => userPermission.PermissionId).HasColumnName("permission_id");
        builder.Property(userPermission => userPermission.GrantedAtUtc).HasColumnName("granted_at_utc").IsRequired();
        builder.Property(userPermission => userPermission.GrantedByUserId).HasColumnName("granted_by_user_id");

        builder.HasOne(userPermission => userPermission.User)
            .WithMany(user => user.UserPermissions)
            .HasForeignKey(userPermission => userPermission.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(userPermission => userPermission.Permission)
            .WithMany(permission => permission.UserPermissions)
            .HasForeignKey(userPermission => userPermission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(userPermission => userPermission.GrantedByUser)
            .WithMany(user => user.GrantedUserPermissions)
            .HasForeignKey(userPermission => userPermission.GrantedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
