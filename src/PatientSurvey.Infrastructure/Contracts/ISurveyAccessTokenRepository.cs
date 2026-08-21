using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface ISurveyAccessTokenRepository :
    IRepositoryBase<SurveyAccessToken>
{
    Task<SurveyAccessToken?> GetTokenWithSurveyAsync(string token, bool trackChanges, CancellationToken cancellationToken);
    Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken);
    Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken);
    void CreateOneSurveyAccessToken(SurveyAccessToken accessToken);
    void UpdateOneSurveyAccessToken(SurveyAccessToken accessToken);
}
