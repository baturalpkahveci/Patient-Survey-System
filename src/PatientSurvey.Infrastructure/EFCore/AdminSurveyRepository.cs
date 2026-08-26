using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class AdminSurveyRepository : IAdminSurveyRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public AdminSurveyRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<Survey>> GetAllSurveysWithQuestionsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Surveys.GetAllSurveys(trackChanges: false)
            .Include(survey => survey.Questions)
            .Include(survey => survey.Department)
            .Include(survey => survey.Doctor)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyResponse)
                    .ThenInclude(response => response!.Answers)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Survey?> GetSurveyWithQuestionsAsync(int surveyId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Surveys.GetOneSurveyByIdAsync(surveyId, trackChanges: false, cancellationToken);
    }

    public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Surveys.GetOneSurveyByIdAsync(surveyId, trackChanges: true, cancellationToken);
    }

    public void AddSurvey(Survey survey)
    {
        _repositoryManager.Surveys.CreateOneSurvey(survey);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
