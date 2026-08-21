using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using AppIAppTransaction = PatientSurvey.Application.Interfaces.IAppTransaction;
using AppISurveyAccessTokenRepository = PatientSurvey.Application.Interfaces.ISurveyAccessTokenRepository;
using AppISurveyReadRepository = PatientSurvey.Application.Interfaces.ISurveyReadRepository;
using AppISurveySubmissionRepository = PatientSurvey.Application.Interfaces.ISurveySubmissionRepository;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class SurveyWorkflowRepository :
    AppISurveySubmissionRepository,
    AppISurveyReadRepository,
    AppISurveyAccessTokenRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public SurveyWorkflowRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public Task<AppIAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.BeginTransactionAsync(cancellationToken);
    }

    public Task<SurveyAccessToken?> GetTokenWithSurveyAsync(string token, CancellationToken cancellationToken)
    {
        return _repositoryManager.SurveyAccessTokens.GetTokenWithSurveyAsync(
            token,
            trackChanges: true,
            cancellationToken);
    }

    public Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken)
    {
        return _repositoryManager.SurveyAccessTokens.GetTokenWithActiveSurveyAsync(token, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SurveyAccessToken>> GetAllTokensWithSurveysAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.SurveyAccessTokens
            .FindAll(trackChanges: false)
            .Include(accessToken => accessToken.Survey)
            .Include(accessToken => accessToken.SurveyResponse)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Surveys.GetOneSurveyByIdAsync(surveyId, trackChanges: false, cancellationToken);
    }

    public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments.GetActiveDepartmentsAsync(cancellationToken);
    }

    public Task<Department?> GetDepartmentAsync(int departmentId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments.GetOneDepartmentByIdAsync(
            departmentId,
            trackChanges: true,
            cancellationToken);
    }

    public void AddSurveyResponse(SurveyResponse response)
    {
        _repositoryManager.SurveyResponses.CreateOneSurveyResponse(response);
    }

    public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken)
    {
        return _repositoryManager.SurveyAccessTokens.TokenExistsAsync(token, cancellationToken);
    }

    public void AddToken(SurveyAccessToken token)
    {
        _repositoryManager.SurveyAccessTokens.CreateOneSurveyAccessToken(token);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
