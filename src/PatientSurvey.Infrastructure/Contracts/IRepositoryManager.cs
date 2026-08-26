using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IRepositoryManager
{
    ISurveyRepository Surveys { get; }
    ISurveyAccessTokenRepository SurveyAccessTokens { get; }
    IDepartmentRepository Departments { get; }
    IQuestionRepository Questions { get; }
    ISurveyResponseRepository SurveyResponses { get; }
    IRoleRepository Roles { get; }
    IUserRepository Users { get; }
    IDoctorRepository Doctors { get; }
    IPatientRepository Patients { get; }
    IPatientVisitRepository PatientVisits { get; }
    ISurveyInvitationRepository SurveyInvitations { get; }
    ISurveyConsentRepository SurveyConsents { get; }
    IAuditLogRepository AuditLogs { get; }
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
}
