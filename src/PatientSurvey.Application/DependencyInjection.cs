using Microsoft.Extensions.DependencyInjection;
using PatientSurvey.Application.Services;

namespace PatientSurvey.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SurveyService>();
        services.AddScoped<SurveySubmissionService>();
        services.AddScoped<SurveyAccessTokenService>();
        services.AddScoped<DepartmentService>();
        services.AddScoped<QuestionService>();
        services.AddScoped<UserService>();
        services.AddScoped<ReportService>();

        return services;
    }
}
