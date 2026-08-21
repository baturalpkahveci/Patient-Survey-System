using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class SurveyAccessTokenRepository :
    RepositoryBase<SurveyAccessToken>,
    ISurveyAccessTokenRepository
{
    public SurveyAccessTokenRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<SurveyAccessToken?> GetTokenWithSurveyAsync(
        string token,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return FindByCondition(accessToken => accessToken.Token == token, trackChanges)
            .Include(accessToken => accessToken.SurveyResponse)
            .Include(accessToken => accessToken.Survey!)
                .ThenInclude(survey => survey.Questions)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken)
    {
        return FindByCondition(
                accessToken => accessToken.Token == token
                    && accessToken.Survey != null
                    && accessToken.Survey.IsActive,
                trackChanges: false)
            .Include(accessToken => accessToken.Survey!)
                .ThenInclude(survey => survey.Questions)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken)
    {
        return FindByCondition(accessToken => accessToken.Token == token, trackChanges: false)
            .AnyAsync(cancellationToken);
    }

    public void CreateOneSurveyAccessToken(SurveyAccessToken accessToken)
    {
        Create(accessToken);
    }

    public void UpdateOneSurveyAccessToken(SurveyAccessToken accessToken)
    {
        Update(accessToken);
    }
}
