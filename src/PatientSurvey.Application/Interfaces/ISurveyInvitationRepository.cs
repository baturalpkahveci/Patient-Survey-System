using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface ISurveyInvitationRepository
{
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<Survey?> GetSurveyByIdAsync(int surveyId, bool trackChanges, CancellationToken cancellationToken);
    Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken);
    Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Doctor>> GetActiveDoctorsByDepartmentAsync(int departmentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken);
    Task<PatientVisit?> GetPatientVisitByIdAsync(int patientVisitId, bool trackChanges, CancellationToken cancellationToken);
    Task<Patient?> GetPatientByTcHashAsync(string tcIdentityLookupHash, CancellationToken cancellationToken);
    Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken);
    void AddPatient(Patient patient);
    void AddPatientVisit(PatientVisit visit);
    void AddSurveyInvitation(SurveyInvitation invitation);
    void AddToken(SurveyAccessToken token);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
