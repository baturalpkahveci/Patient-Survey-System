using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.UnitTests;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task GetAuditLogsAsync_orders_newest_first_and_maps_all_fields()
    {
        var older = new AuditLog
        {
            Id = 1,
            OccurredAtUtc = new DateTimeOffset(2026, 8, 20, 8, 0, 0, TimeSpan.Zero),
            UserId = 10,
            Username = "admin",
            UserRole = "Admin",
            Action = "Ekleme",
            EntityName = "Soru",
            EntityId = "4",
            Summary = "Soru eklendi",
            ChangesJson = """{"Text":"Yeni soru"}""",
            IpAddress = "127.0.0.1",
            RequestPath = "/Admin/Questions/Create"
        };
        var newer = new AuditLog
        {
            Id = 2,
            OccurredAtUtc = older.OccurredAtUtc.AddMinutes(5),
            Username = "manager",
            Action = "Güncelleme",
            EntityName = "Kullanıcı",
            Summary = "Kullanıcı güncellendi"
        };
        var service = new AuditLogService(new FakeAuditLogRepository(new[] { older, newer }));

        var result = await service.GetAuditLogsAsync();

        Assert.Equal(new[] { 2, 1 }, result.Select(log => log.Id));
        var mapped = result.Last();
        Assert.Equal(10, mapped.UserId);
        Assert.Equal("admin", mapped.Username);
        Assert.Equal("Admin", mapped.UserRole);
        Assert.Equal("Ekleme", mapped.Action);
        Assert.Equal("Soru", mapped.EntityName);
        Assert.Equal("4", mapped.EntityId);
        Assert.Equal("Soru eklendi", mapped.Summary);
        Assert.Equal("""{"Text":"Yeni soru"}""", mapped.ChangesJson);
        Assert.Equal("127.0.0.1", mapped.IpAddress);
        Assert.Equal("/Admin/Questions/Create", mapped.RequestPath);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        private readonly IReadOnlyCollection<AuditLog> _logs;

        public FakeAuditLogRepository(IReadOnlyCollection<AuditLog> logs)
        {
            _logs = logs;
        }

        public Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_logs);
        }
    }
}
