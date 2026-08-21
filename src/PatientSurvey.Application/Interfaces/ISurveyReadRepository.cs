using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface ISurveyReadRepository
{
    Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken);
}
