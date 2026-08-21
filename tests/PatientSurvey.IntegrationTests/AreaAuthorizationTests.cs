using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Manager_dashboard_allows_manager_role()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Manager");

        var response = await client.GetAsync("/Manager/Dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.DashboardController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.UsersController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.SurveysController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.QuestionsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.DepartmentsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.TokensController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.ResultsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Admin.Controllers.ReportsController), "Admin")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.DashboardController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.TokensController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.ResultsController), "Manager")]
    [InlineData(typeof(PatientSurvey.WebUI.Areas.Manager.Controllers.ReportsController), "Manager")]
    public void Management_controllers_declare_expected_role_boundary(Type controllerType, string expectedRoles)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(expectedRoles, authorize!.Roles);
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
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=patient_survey_test;Username=test;Password=test"
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
