using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface ISurveyAccessTokenRepository
{
    Task<IReadOnlyCollection<SurveyAccessToken>> GetAllTokensWithSurveysAsync(
        bool includePatientPersonalData,
        CancellationToken cancellationToken);
    Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken);
    Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken);
    void AddToken(SurveyAccessToken token);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
