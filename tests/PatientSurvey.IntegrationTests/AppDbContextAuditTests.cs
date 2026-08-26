using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.IntegrationTests;

public sealed class AppDbContextAuditTests
{
    [Fact]
    public async Task SaveChangesAsync_creates_audit_log_for_added_entity_with_current_user_context_and_generated_id()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            RoleId = 1,
            Username = "doctor1",
            PasswordHash = "plain-secret",
            IsActive = true
        });

        await context.SaveChangesAsync();

        var auditLog = Assert.Single(context.AuditLogs);
        Assert.Equal("Ekleme", auditLog.Action);
        Assert.Equal("Kullanıcı", auditLog.EntityName);
        Assert.Equal("1", auditLog.EntityId);
        Assert.Equal(42, auditLog.UserId);
        Assert.Equal("admin", auditLog.Username);
        Assert.Equal("Admin", auditLog.UserRole);
        Assert.Equal("10.0.0.5", auditLog.IpAddress);
        Assert.Equal("/Admin/Users/Create", auditLog.RequestPath);
        Assert.Contains("PasswordHash", auditLog.ChangesJson);
        Assert.Contains("[gizli]", auditLog.ChangesJson);
        Assert.DoesNotContain("plain-secret", auditLog.ChangesJson);
    }

    [Fact]
    public async Task SaveChangesAsync_logs_only_changed_fields_for_modified_entity_and_masks_sensitive_changes()
    {
        await using var context = CreateContext();
        var user = new User
        {
            Id = 7,
            RoleId = 1,
            Username = "manager1",
            PasswordHash = "old-secret",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();

        user.IsActive = false;
        await context.SaveChangesAsync();

        var statusAuditLog = Assert.Single(context.AuditLogs);
        Assert.Equal("Güncelleme", statusAuditLog.Action);
        Assert.Contains("IsActive", statusAuditLog.ChangesJson);
        Assert.DoesNotContain("PasswordHash", statusAuditLog.ChangesJson);

        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();
        user.PasswordHash = "new-secret";
        await context.SaveChangesAsync();

        var passwordAuditLog = Assert.Single(context.AuditLogs);
        Assert.Equal("Güncelleme", passwordAuditLog.Action);
        Assert.Contains("PasswordHash", passwordAuditLog.ChangesJson);
        Assert.Contains("[gizli]", passwordAuditLog.ChangesJson);
        Assert.DoesNotContain("old-secret", passwordAuditLog.ChangesJson);
        Assert.DoesNotContain("new-secret", passwordAuditLog.ChangesJson);
    }

    [Fact]
    public async Task SaveChangesAsync_creates_delete_audit_log_without_auditing_audit_log_cleanup()
    {
        await using var context = CreateContext();
        var department = new Department { Id = 5, Name = "Acil", IsActive = true };
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();

        context.Departments.Remove(department);
        await context.SaveChangesAsync();

        var auditLog = Assert.Single(context.AuditLogs);
        Assert.Equal("Silme", auditLog.Action);
        Assert.Equal("Bölüm", auditLog.EntityName);
        Assert.Equal("5", auditLog.EntityId);
        Assert.Contains("Acil", auditLog.Summary);
    }

    [Fact]
    public async Task SaveChangesAsync_refreshes_added_related_entities_with_real_foreign_keys()
    {
        await using var context = CreateContext();
        var patient = new Patient
        {
            FirstName = "Emre",
            LastName = "Aktas",
            TcIdentityLookupHash = "hash",
            PhoneNumber = "05551234567",
            Email = "hasta@example.test"
        };
        var visit = new PatientVisit
        {
            Patient = patient,
            CreatedByUserId = 42,
            DoctorId = 1,
            DepartmentId = 2
        };
        var invitation = new SurveyInvitation
        {
            SurveyId = 14,
            PatientVisit = visit,
            CreatedByUserId = 42,
            DeliveryMethod = SurveyDeliveryMethod.Sms,
            DeliveryStatus = SurveyDeliveryStatus.LinkCreated
        };
        var token = new SurveyAccessToken
        {
            SurveyId = 14,
            SurveyInvitation = invitation,
            Token = "secret-token"
        };

        context.AddRange(patient, visit, invitation, token);
        await context.SaveChangesAsync();

        var auditLogs = context.AuditLogs.OrderBy(log => log.Id).ToArray();

        Assert.Equal(4, auditLogs.Length);
        Assert.All(auditLogs, log => Assert.DoesNotContain("#0", log.Summary));
        Assert.All(auditLogs, log => Assert.DoesNotContain("#1 #1", log.Summary));
        Assert.All(auditLogs, log => Assert.DoesNotContain("-214748", log.ChangesJson ?? string.Empty));
        Assert.All(auditLogs, log => Assert.DoesNotContain("Emre", log.Summary));
        Assert.All(auditLogs, log => Assert.DoesNotContain("Emre", log.ChangesJson ?? string.Empty));
        Assert.All(auditLogs, log => Assert.DoesNotContain("Aktas", log.ChangesJson ?? string.Empty));
        Assert.All(auditLogs, log => Assert.DoesNotContain("05551234567", log.ChangesJson ?? string.Empty));
        Assert.All(auditLogs, log => Assert.DoesNotContain("hasta@example.test", log.ChangesJson ?? string.Empty));
        Assert.Contains(auditLogs, log =>
            log.EntityName == "Hasta Daveti" &&
            log.Summary == "Hasta Daveti: Davet #1 için ekleme işlemi yapıldı.");
        Assert.Contains(auditLogs, log =>
            log.EntityName == "Hasta Kaydı" &&
            log.ChangesJson!.Contains("\"PatientId\":1", StringComparison.Ordinal));
        Assert.Contains(auditLogs, log =>
            log.EntityName == "Hasta Daveti" &&
            log.ChangesJson!.Contains("\"PatientVisitId\":1", StringComparison.Ordinal));
        Assert.Contains(auditLogs, log =>
            log.EntityName == "Anket Linki" &&
            log.ChangesJson!.Contains("\"SurveyInvitationId\":1", StringComparison.Ordinal) &&
            !log.ChangesJson.Contains("secret-token", StringComparison.Ordinal));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new FakeCurrentUserContext());
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public int? UserId => 42;
        public string Username => "admin";
        public string? Role => "Admin";
        public string? IpAddress => "10.0.0.5";
        public string? RequestPath => "/Admin/Users/Create";
    }
}
