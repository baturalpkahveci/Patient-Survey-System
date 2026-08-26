using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Security;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.IntegrationTests;

public sealed class AreaAuthorizationTests : IClassFixture<AreaAuthorizationTests.Factory>
{
    private readonly Factory _factory;

    public AreaAuthorizationTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_dashboard_allows_admin_role()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");

        var response = await client.GetAsync("/Admin/Dashboard");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Sistem Geçmişi", body);
        Assert.Contains("/Admin/SystemHistory", body);
    }

    [Fact]
    public async Task Manager_dashboard_allows_manager_role()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Manager");

        var response = await client.GetAsync("/Manager/Dashboard");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task Doctor_patient_visits_without_permission_does_not_show_patient_personal_data()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Doctor");

        var response = await client.GetAsync("/Doctor/PatientVisits");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Hasta #2", body);
        Assert.DoesNotContain("Emre Aktaş", body);
        Assert.DoesNotContain("emre@example.test", body);
        Assert.DoesNotContain("5551002030", body);
    }

    [Fact]
    public async Task Doctor_patient_visits_does_not_show_patient_personal_data_even_when_permission_header_is_sent()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Doctor");
        client.DefaultRequestHeaders.Add(TestAuthHandler.PatientPiiPermissionHeader, "true");

        var response = await client.GetAsync("/Doctor/PatientVisits");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Hasta #2", body);
        Assert.DoesNotContain("Emre", body);
        Assert.DoesNotContain("Akta", body);
        Assert.DoesNotContain("emre@example.test", body);
        Assert.DoesNotContain("5551002030", body);
    }

    [Fact]
    public async Task Admin_patient_visits_without_permission_does_not_show_patient_personal_data()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");

        var response = await client.GetAsync("/Admin/PatientVisits");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Hasta #2", body);
        Assert.DoesNotContain("Emre", body);
        Assert.DoesNotContain("Akta", body);
        Assert.DoesNotContain("emre@example.test", body);
        Assert.DoesNotContain("5551002030", body);
    }

    [Fact]
    public async Task Admin_patient_visits_with_permission_shows_patient_personal_data()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");
        client.DefaultRequestHeaders.Add(TestAuthHandler.PatientPiiPermissionHeader, "true");

        var response = await client.GetAsync("/Admin/PatientVisits");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Emre", body);
        Assert.Contains("Akta", body);
        Assert.Contains("emre@example.test", body);
        Assert.Contains("5551002030", body);
    }

    [Fact]
    public async Task Manager_patient_visits_without_permission_does_not_show_patient_personal_data()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Manager");

        var response = await client.GetAsync("/Manager/PatientVisits");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Hasta #2", body);
        Assert.DoesNotContain("Emre", body);
        Assert.DoesNotContain("Akta", body);
        Assert.DoesNotContain("emre@example.test", body);
        Assert.DoesNotContain("5551002030", body);
    }

    [Fact]
    public async Task Doctor_dashboard_allows_doctor_role()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Doctor");

        var response = await client.GetAsync("/Doctor/Dashboard");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task Admin_system_history_lists_audit_logs_when_action_filter_is_empty()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");

        var response = await client.GetAsync("/Admin/SystemHistory");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Contains("Question added", body);
        Assert.Contains("User deactivated", body);
    }

    [Theory]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Admin/Results")]
    [InlineData("/Admin/Users")]
    [InlineData("/Admin/PatientVisits")]
    public async Task Doctor_role_cannot_access_admin_endpoints(string path)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Doctor");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.DashboardController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.UsersController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.SurveysController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.QuestionsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.DepartmentsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.DoctorsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.TokensController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.PatientVisitsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.ResultsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.SystemHistoryController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.DashboardController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.TokensController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.PatientVisitsController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.ResultsController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.ReportsController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.DashboardController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.SurveysController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.QuestionsController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.PatientsController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.PatientVisitsController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.TokensController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.ResultsController), "Doctor")]
    public void Management_controllers_declare_expected_role_boundary(Type controllerType, string expectedRoles)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(expectedRoles, authorize!.Roles);
    }

    [Fact]
    public void System_history_action_filter_does_not_bind_from_route_action()
    {
        var method = typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.SystemHistoryController)
            .GetMethod(nameof(PatientSurvey.WebUI.Areas.Admin.Controllers.SystemHistoryController.Index));

        Assert.NotNull(method);
        Assert.DoesNotContain(method!.GetParameters(), parameter => parameter.Name == "action");

        var actionFilter = method.GetParameters().Single(parameter => parameter.Name == "actionFilter");
        var fromQuery = actionFilter.GetCustomAttribute<FromQueryAttribute>();

        Assert.NotNull(fromQuery);
        Assert.Equal("Action", fromQuery!.Name);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=patient_survey_test;Username=test;Password=test",
                    ["PATIENT_IDENTITY_KEY"] = "integration-test-identity-key"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });

                services.AddScoped<IManagementReportRepository, EmptyReportRepository>();
                services.AddScoped<IDoctorManagementRepository, EmptyDoctorManagementRepository>();
                services.AddScoped<IAuditLogRepository, SampleAuditLogRepository>();
                services.AddScoped<IPatientVisitReadRepository, SamplePatientVisitReadRepository>();
                services.AddScoped<IPermissionRepository, SamplePermissionRepository>();
            });
        }
    }

    private sealed class SamplePatientVisitReadRepository : IPatientVisitReadRepository
    {
        public Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsAsync(
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PatientVisit>>(BuildVisits(includePatientPersonalData));
        }

        public Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsByDoctorAsync(
            int doctorId,
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PatientVisit>>(BuildVisits(includePatientPersonalData)
                .Where(visit => visit.DoctorId == doctorId)
                .ToArray());
        }

        private static PatientVisit[] BuildVisits(bool includePatientPersonalData)
        {
            var department = new Department { Id = 1, Name = "Kardiyoloji", IsActive = true };
            var doctor = new Doctor
            {
                Id = 1,
                UserId = 1,
                FirstName = "Test",
                LastName = "Doktor",
                DepartmentId = department.Id,
                Department = department,
                IsActive = true
            };

            return new[]
            {
                new PatientVisit
                {
                    Id = 7,
                    PatientId = 2,
                    Patient = includePatientPersonalData
                        ? new Patient
                        {
                            Id = 2,
                            FirstName = "Emre",
                            LastName = "Aktaş",
                            PhoneNumber = "5551002030",
                            Email = "emre@example.test"
                        }
                        : null,
                    DoctorId = doctor.Id,
                    Doctor = doctor,
                    DepartmentId = department.Id,
                    Department = department,
                    CreatedByUser = new User { Id = 1, Username = "doctor" },
                    ExaminedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
                    Invitations =
                    {
                        new SurveyInvitation
                        {
                            Id = 9,
                            DeliveryStatus = SurveyDeliveryStatus.LinkCreated,
                            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                            Survey = new Survey { Id = 14, Title = "Kontrol Anketi" }
                        }
                    }
                }
            };
        }
    }

    private sealed class SampleAuditLogRepository : IAuditLogRepository
    {
        public Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AuditLog>>(new[]
            {
                new AuditLog
                {
                    Id = 1,
                    OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Username = "admin",
                    UserRole = "Admin",
                    Action = "Ekleme",
                    EntityName = "Soru",
                    EntityId = "1",
                    Summary = "Question added"
                },
                new AuditLog
                {
                    Id = 2,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    Username = "admin",
                    UserRole = "Admin",
                    Action = "Güncelleme",
                    EntityName = "Kullanıcı",
                    EntityId = "2",
                    Summary = "User deactivated"
                }
            });
        }

        public void AddAuditLog(AuditLog auditLog)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class SamplePermissionRepository : IPermissionRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SamplePermissionRepository(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<User?> GetUserPermissionProfileAsync(
            int userId,
            bool trackChanges,
            CancellationToken cancellationToken)
        {
            var roleName = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";
            var hasPermission = string.Equals(
                _httpContextAccessor.HttpContext?.Request.Headers[TestAuthHandler.PatientPiiPermissionHeader].ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase);

            var permission = new Permission
            {
                Id = 1,
                Name = AppPermissions.CanViewPatientPersonalData,
                IsActive = true
            };
            var user = new User
            {
                Id = userId,
                Username = roleName.ToLowerInvariant(),
                Role = new Role { Name = roleName },
                IsActive = true
            };

            if (hasPermission)
            {
                user.UserPermissions.Add(new UserPermission
                {
                    UserId = user.Id,
                    PermissionId = permission.Id,
                    Permission = permission
                });
            }

            return Task.FromResult<User?>(user);
        }

        public Task<IReadOnlyCollection<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Permission>>(new[]
            {
                new Permission
                {
                    Id = 1,
                    Name = AppPermissions.CanViewPatientPersonalData,
                    Description = "Hasta kişisel verilerini görüntüleyebilir.",
                    IsActive = true
                }
            });
        }

        public Task<Permission?> GetActivePermissionByNameAsync(string permissionName, CancellationToken cancellationToken)
        {
            return Task.FromResult<Permission?>(new Permission
            {
                Id = 1,
                Name = permissionName,
                IsActive = true
            });
        }

        public void AddUserPermission(UserPermission userPermission)
        {
        }

        public void RemoveUserPermission(UserPermission userPermission)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class EmptyReportRepository : IManagementReportRepository
    {
        public Task<IReadOnlyCollection<Survey>> GetSurveysForDashboardAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Survey>>(Array.Empty<Survey>());
        }

        public Task<IReadOnlyCollection<SurveyResponse>> GetResponsesForResultsAsync(
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<SurveyResponse>>(Array.Empty<SurveyResponse>());
        }

        public Task<SurveyResponse?> GetResponseDetailAsync(
            int responseId,
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<SurveyResponse?>(null);
        }

        public Task<IReadOnlyCollection<Survey>> GetSurveysForReportsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Survey>>(Array.Empty<Survey>());
        }

        public Task<IReadOnlyCollection<Doctor>> GetDoctorsForReportsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Doctor>>(Array.Empty<Doctor>());
        }
    }

    private sealed class EmptyDoctorManagementRepository : IDoctorManagementRepository
    {
        private readonly Department _department = new() { Id = 1, Name = "Kardiyoloji", IsActive = true };
        private readonly User _doctorUser = new()
        {
            Id = 1,
            Username = "doctor",
            RoleId = 3,
            Role = new Role { Id = 3, Name = "Doctor", IsActive = true },
            IsActive = true
        };

        public Task<IReadOnlyCollection<Doctor>> GetAllDoctorsWithDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Doctor>>(new[]
            {
                new Doctor
                {
                    Id = 1,
                    UserId = 1,
                    FirstName = "Test",
                    LastName = "Doktor",
                    DepartmentId = 1,
                    Department = _department,
                    User = _doctorUser,
                    IsActive = true
                }
            });
        }

        public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Doctor?>(new Doctor
            {
                Id = doctorId,
                UserId = 1,
                FirstName = "Test",
                LastName = "Doktor",
                DepartmentId = 1,
                Department = _department,
                User = _doctorUser,
                IsActive = true
            });
        }

        public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return GetDoctorByIdAsync(1, cancellationToken);
        }

        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(new[] { _department });
        }

        public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Department?>(departmentId == _department.Id ? _department : null);
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(userId == _doctorUser.Id ? _doctorUser : null);
        }

        public void AddDoctor(Doctor doctor)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";
        public const string RoleHeader = "X-Test-Role";
        public const string PatientPiiPermissionHeader = "X-Test-CanViewPatientPii";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RoleHeader, out var roleValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var role = roleValues.ToString();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, role.ToLowerInvariant()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    }
}
