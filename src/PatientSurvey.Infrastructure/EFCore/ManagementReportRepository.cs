using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class ManagementReportRepository : IManagementReportRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public ManagementReportRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<Survey>> GetSurveysForDashboardAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Surveys
            .FindAll(trackChanges: false)
            .Include(survey => survey.Questions)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyResponse)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SurveyResponse>> GetResponsesForResultsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.SurveyResponses
            .FindAll(trackChanges: false)
            .Include(response => response.Department)
            .Include(response => response.Answers)
            .Include(response => response.Token!)
                .ThenInclude(token => token.Survey)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SurveyResponse?> GetResponseDetailAsync(int responseId, CancellationToken cancellationToken)
    {
        return _repositoryManager.SurveyResponses
            .FindByCondition(response => response.Id == responseId, trackChanges: false)
            .Include(response => response.Department)
            .Include(response => response.Token!)
                .ThenInclude(token => token.Survey)
            .Include(response => response.Answers)
                .ThenInclude(answer => answer.Question)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Survey>> GetSurveysForReportsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Surveys
            .FindAll(trackChanges: false)
            .Include(survey => survey.Questions)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyResponse!)
                    .ThenInclude(response => response.Department)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyResponse!)
                    .ThenInclude(response => response.Answers)
            .ToArrayAsync(cancellationToken);
    }
}
