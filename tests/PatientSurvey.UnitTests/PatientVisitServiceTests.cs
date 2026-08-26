using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Security;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class PatientVisitServiceTests
{
    [Fact]
    public async Task GetPatientVisitsAsync_without_permission_returns_patient_reference_only()
    {
        var service = new PatientVisitService(new FakePatientVisitReadRepository());

        var visits = await service.GetPatientVisitsAsync();

        var visit = Assert.Single(visits);
        Assert.Equal("Hasta #5", visit.PatientName);
        Assert.Equal("Hasta #5", visit.MaskedPatientName);
        Assert.Null(visit.PatientPhone);
        Assert.Null(visit.PatientEmail);
        Assert.Equal("Dr. Ayşe Kaya", visit.DoctorName);
        Assert.Equal("Gönderildi", visit.LatestDeliveryStatusLabel);
        Assert.Equal("Kontrol Anketi", visit.LatestSurveyTitle);
    }

    [Fact]
    public async Task GetPatientVisitsAsync_with_permission_returns_patient_personal_data()
    {
        var repository = new FakePatientVisitReadRepository();
        var service = new PatientVisitService(
            repository,
            CreatePermissionService(roleName: "Admin", includePermission: true));

        var visits = await service.GetPatientVisitsAsync();

        var visit = Assert.Single(visits);
        Assert.Equal("Emre Aktaş", visit.PatientName);
        Assert.Equal("Em*** Ak***", visit.MaskedPatientName);
        Assert.Equal("5551002030", visit.PatientPhone);
        Assert.Equal("emre@example.test", visit.PatientEmail);
        Assert.True(repository.IncludePatientPersonalData);
    }

    [Fact]
    public async Task GetPatientVisitsAsync_doctor_without_permission_does_not_request_patient_personal_data()
    {
        var repository = new FakePatientVisitReadRepository();
        var service = new PatientVisitService(
            repository,
            CreatePermissionService(roleName: "Doctor", includePermission: false));

        var visits = await service.GetPatientVisitsAsync();

        var visit = Assert.Single(visits);
        Assert.Equal("Hasta #5", visit.PatientName);
        Assert.False(repository.IncludePatientPersonalData);
    }

    [Fact]
    public async Task GetPatientVisitsByDoctorAsync_uses_repository_scope()
    {
        var repository = new FakePatientVisitReadRepository();
        var service = new PatientVisitService(repository);

        var visits = await service.GetPatientVisitsByDoctorAsync(7);

        Assert.True(repository.DoctorScopeWasUsed);
        Assert.All(visits, visit => Assert.Equal(7, visit.DoctorId));
    }

    private sealed class FakePatientVisitReadRepository : IPatientVisitReadRepository
    {
        public bool DoctorScopeWasUsed { get; private set; }
        public bool? IncludePatientPersonalData { get; private set; }

        public Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsAsync(
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            IncludePatientPersonalData = includePatientPersonalData;
            return Task.FromResult<IReadOnlyCollection<PatientVisit>>(BuildVisits());
        }

        public Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsByDoctorAsync(
            int doctorId,
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            DoctorScopeWasUsed = true;
            IncludePatientPersonalData = includePatientPersonalData;
            return Task.FromResult<IReadOnlyCollection<PatientVisit>>(BuildVisits()
                .Where(visit => visit.DoctorId == doctorId)
                .ToArray());
        }

        private static PatientVisit[] BuildVisits()
        {
            var department = new Department { Id = 3, Name = "Kardiyoloji", IsActive = true };
            var doctor = new Doctor
            {
                Id = 7,
                FirstName = "Ayşe",
                LastName = "Kaya",
                DepartmentId = department.Id,
                Department = department
            };
            var visit = new PatientVisit
            {
                Id = 11,
                PatientId = 5,
                Patient = new Patient
                {
                    Id = 5,
                    FirstName = "Emre",
                    LastName = "Aktaş",
                    PhoneNumber = "5551002030",
                    Email = "emre@example.test"
                },
                DoctorId = doctor.Id,
                Doctor = doctor,
                DepartmentId = department.Id,
                Department = department,
                CreatedByUser = new User { Id = 2, Username = "doctor" },
                ExaminedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                Invitations =
                {
                    new SurveyInvitation
                    {
                        Id = 9,
                        DeliveryStatus = SurveyDeliveryStatus.Sent,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        Survey = new Survey { Id = 4, Title = "Kontrol Anketi" }
                    }
                }
            };

            return new[] { visit };
        }
    }

    private static PermissionService CreatePermissionService(string roleName, bool includePermission)
    {
        return new PermissionService(
            new FakePermissionRepository(roleName, includePermission),
            new FakeAuditLogRepository(),
            new FakeCurrentUserContext(roleName));
    }

    private sealed class FakePermissionRepository : IPermissionRepository
    {
        private readonly string _roleName;
        private readonly bool _includePermission;

        public FakePermissionRepository(string roleName, bool includePermission)
        {
            _roleName = roleName;
            _includePermission = includePermission;
        }

        public Task<User?> GetUserPermissionProfileAsync(
            int userId,
            bool trackChanges,
            CancellationToken cancellationToken)
        {
            var permission = new Permission
            {
                Id = 1,
                Name = AppPermissions.CanViewPatientPersonalData,
                IsActive = true
            };

            var user = new User
            {
                Id = userId,
                Username = _roleName.ToLowerInvariant(),
                IsActive = true,
                Role = new Role { Name = _roleName }
            };

            if (_includePermission)
            {
                user.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = permission.Id,
                    Permission = permission
                });
            }

            return Task.FromResult<User?>(user);
        }

        public Task<IReadOnlyCollection<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Permission>>(Array.Empty<Permission>());
        }

        public Task<Permission?> GetActivePermissionByNameAsync(
            string permissionName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Permission?>(null);
        }

        public void AddUserPermission(UserPermission userPermission)
        {
        }

        public void RemoveUserPermission(UserPermission userPermission)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AuditLog>>(Array.Empty<AuditLog>());
        }

        public void AddAuditLog(AuditLog auditLog)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public FakeCurrentUserContext(string roleName)
        {
            Role = roleName;
            Username = roleName.ToLowerInvariant();
        }

        public int? UserId => 1;
        public string Username { get; }
        public string? Role { get; }
        public string? IpAddress => "127.0.0.1";
        public string? RequestPath => "/test";
    }
}
