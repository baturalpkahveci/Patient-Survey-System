using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PatientSurvey.IntegrationTests;

public sealed class SurveyThankYouRouteTests : IClassFixture<SurveyThankYouRouteTests.Factory>
{
    private readonly Factory _factory;

    public SurveyThankYouRouteTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Thank_you_route_renders_success_page()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Survey/ThankYou");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("success-panel", body);
        Assert.Contains("Anketiniz basariyla gonderildi", body);
        Assert.DoesNotContain("Anket bağlantısı kullanılamıyor", body);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=patient_survey_test;Username=test;Password=test"
                });
            });
        }
    }
}
