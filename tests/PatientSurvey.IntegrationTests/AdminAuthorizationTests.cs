using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PatientSurvey.IntegrationTests;

public sealed class AdminAuthorizationTests : IClassFixture<AdminAuthorizationTests.Factory>
{
    private readonly Factory _factory;

    public AdminAuthorizationTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_dashboard_requires_authentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/Admin/Dashboard");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=patient_survey_test;Username=test;Password=test",
                    ["PATIENT_IDENTITY_KEY"] = "integration-test-identity-key"
                });
            });
        }
    }
}
