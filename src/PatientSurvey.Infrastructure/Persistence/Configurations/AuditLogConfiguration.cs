using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id).HasColumnName("id");
        builder.Property(log => log.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Property(log => log.UserId).HasColumnName("user_id");
        builder.Property(log => log.Username).HasColumnName("username").HasMaxLength(120).IsRequired();
        builder.Property(log => log.UserRole).HasColumnName("user_role").HasMaxLength(80);
        builder.Property(log => log.Action).HasColumnName("action").HasMaxLength(40).IsRequired();
        builder.Property(log => log.EntityName).HasColumnName("entity_name").HasMaxLength(120).IsRequired();
        builder.Property(log => log.EntityId).HasColumnName("entity_id").HasMaxLength(80);
        builder.Property(log => log.Summary).HasColumnName("summary").HasMaxLength(1000).IsRequired();
        builder.Property(log => log.ChangesJson).HasColumnName("changes_json").HasColumnType("jsonb");
        builder.Property(log => log.IpAddress).HasColumnName("ip_address").HasMaxLength(80);
        builder.Property(log => log.RequestPath).HasColumnName("request_path").HasMaxLength(300);

        builder.HasIndex(log => log.OccurredAtUtc);
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.Username);
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => log.EntityName);

        builder.HasOne(log => log.User)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
