using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.EFCore;
using PatientSurvey.Infrastructure.Persistence;
using PatientSurvey.Infrastructure.Services;
using AppIClock = PatientSurvey.Application.Interfaces.IClock;
using AppIAdminDepartmentRepository = PatientSurvey.Application.Interfaces.IAdminDepartmentRepository;
using AppIAdminQuestionRepository = PatientSurvey.Application.Interfaces.IAdminQuestionRepository;
using AppIAdminSurveyRepository = PatientSurvey.Application.Interfaces.IAdminSurveyRepository;
using AppIAdminUserRepository = PatientSurvey.Application.Interfaces.IAdminUserRepository;
using AppIManagementReportRepository = PatientSurvey.Application.Interfaces.IManagementReportRepository;
using AppIDoctorManagementRepository = PatientSurvey.Application.Interfaces.IDoctorManagementRepository;
using AppIAuditLogRepository = PatientSurvey.Application.Interfaces.IAuditLogRepository;
using AppIPatientVisitReadRepository = PatientSurvey.Application.Interfaces.IPatientVisitReadRepository;
using AppIEmailSender = PatientSurvey.Application.Interfaces.IEmailSender;
using AppIPatientIdentityProtector = PatientSurvey.Application.Interfaces.IPatientIdentityProtector;
using AppIPasswordHasher = PatientSurvey.Application.Interfaces.IPasswordHasher;
using AppISmsSender = PatientSurvey.Application.Interfaces.ISmsSender;
using AppISurveyAccessTokenRepository = PatientSurvey.Application.Interfaces.ISurveyAccessTokenRepository;
using AppISurveyInvitationRepository = PatientSurvey.Application.Interfaces.ISurveyInvitationRepository;
using AppISurveyReadRepository = PatientSurvey.Application.Interfaces.ISurveyReadRepository;
using AppISurveySubmissionRepository = PatientSurvey.Application.Interfaces.ISurveySubmissionRepository;
using AppIUserRepository = PatientSurvey.Application.Interfaces.IUserRepository;
using InfraISurveyAccessTokenRepository = PatientSurvey.Infrastructure.Contracts.ISurveyAccessTokenRepository;
using InfraIUserRepository = PatientSurvey.Infrastructure.Contracts.IUserRepository;

namespace PatientSurvey.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IRepositoryManager, RepositoryManager>();
        services.AddScoped<ISurveyRepository>(provider => provider.GetRequiredService<IRepositoryManager>().Surveys);
        services.AddScoped<InfraISurveyAccessTokenRepository>(provider =>
            provider.GetRequiredService<IRepositoryManager>().SurveyAccessTokens);
        services.AddScoped<IDepartmentRepository>(provider => provider.GetRequiredService<IRepositoryManager>().Departments);
        services.AddScoped<IQuestionRepository>(provider => provider.GetRequiredService<IRepositoryManager>().Questions);
        services.AddScoped<ISurveyResponseRepository>(provider =>
            provider.GetRequiredService<IRepositoryManager>().SurveyResponses);
        services.AddScoped<IRoleRepository>(provider => provider.GetRequiredService<IRepositoryManager>().Roles);
        services.AddScoped<InfraIUserRepository>(provider => provider.GetRequiredService<IRepositoryManager>().Users);
        services.AddScoped<SurveyWorkflowRepository>();
        services.AddScoped<AppIAdminUserRepository, AdminUserRepository>();
        services.AddScoped<AppIAdminSurveyRepository, AdminSurveyRepository>();
        services.AddScoped<AppIAdminQuestionRepository, AdminQuestionRepository>();
        services.AddScoped<AppIAdminDepartmentRepository, AdminDepartmentRepository>();
        services.AddScoped<AppIManagementReportRepository, ManagementReportRepository>();
        services.AddScoped<AppIDoctorManagementRepository, DoctorManagementRepository>();
        services.AddScoped<AppIAuditLogRepository, AuditLogReadRepository>();
        services.AddScoped<AppIPatientVisitReadRepository, PatientVisitReadRepository>();
        services.AddScoped<AppISurveySubmissionRepository>(provider =>
            provider.GetRequiredService<SurveyWorkflowRepository>());
        services.AddScoped<AppISurveyReadRepository>(provider =>
            provider.GetRequiredService<SurveyWorkflowRepository>());
        services.AddScoped<AppISurveyAccessTokenRepository>(provider =>
            provider.GetRequiredService<SurveyWorkflowRepository>());
        services.AddScoped<AppISurveyInvitationRepository>(provider =>
            provider.GetRequiredService<SurveyWorkflowRepository>());
        services.AddScoped<AppIUserRepository>(provider =>
            (AppIUserRepository)provider.GetRequiredService<InfraIUserRepository>());
        services.AddSingleton<AppIClock, SystemClock>();
        services.AddSingleton<AppIPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<AppIPatientIdentityProtector, HmacPatientIdentityProtector>();
        services.AddSingleton<AppISmsSender, DevelopmentSmsSender>();
        services.AddSingleton<AppIEmailSender, DevelopmentEmailSender>();

        return services;
    }
}
