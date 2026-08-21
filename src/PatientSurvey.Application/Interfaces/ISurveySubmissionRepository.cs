using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface ISurveySubmissionRepository
{
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<SurveyAccessToken?> GetTokenWithSurveyAsync(string token, CancellationToken cancellationToken);
    Task<Department?> GetDepartmentAsync(int departmentId, CancellationToken cancellationToken);
    void AddSurveyResponse(SurveyResponse response);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
