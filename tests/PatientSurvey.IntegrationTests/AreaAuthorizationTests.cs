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
using PatientSurvey.Domain.Entities;

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
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.ResultsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.SystemHistoryController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.DashboardController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.TokensController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.ResultsController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.ReportsController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.DashboardController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.SurveysController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.QuestionsController), "Doctor")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Doctor.Controllers.PatientsController), "Doctor")]
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
            });
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
    }

    private sealed class EmptyReportRepository : IManagementReportRepository
    {
        public Task<IReadOnlyCollection<Survey>> GetSurveysForDashboardAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Survey>>(Array.Empty<Survey>());
        }

        public Task<IReadOnlyCollection<SurveyResponse>> GetResponsesForResultsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<SurveyResponse>>(Array.Empty<SurveyResponse>());
        }

        public Task<SurveyResponse?> GetResponseDetailAsync(int responseId, CancellationToken cancellationToken)
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
