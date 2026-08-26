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
                .ThenInclude(answer => answer.Question)
            .Include(response => response.Token!)
                .ThenInclude(token => token.Survey)
            .Include(response => response.Token!)
                .ThenInclude(token => token.SurveyInvitation!)
                    .ThenInclude(invitation => invitation.PatientVisit!)
                        .ThenInclude(visit => visit.Patient)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SurveyResponse?> GetResponseDetailAsync(int responseId, CancellationToken cancellationToken)
    {
        return _repositoryManager.SurveyResponses
            .FindByCondition(response => response.Id == responseId, trackChanges: false)
            .Include(response => response.Department)
            .Include(response => response.Token!)
                .ThenInclude(token => token.Survey)
            .Include(response => response.Token!)
                .ThenInclude(token => token.SurveyInvitation!)
                    .ThenInclude(invitation => invitation.PatientVisit!)
                        .ThenInclude(visit => visit.Patient)
            .Include(response => response.Answers)
                .ThenInclude(answer => answer.Question)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Survey>> GetSurveysForReportsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Surveys
            .FindAll(trackChanges: false)
            .Include(survey => survey.Questions)
            .Include(survey => survey.Department)
            .Include(survey => survey.Doctor!)
                .ThenInclude(doctor => doctor.Department)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyResponse!)
                    .ThenInclude(response => response.Department)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyResponse!)
                    .ThenInclude(response => response.Answers)
            .Include(survey => survey.AccessTokens)
                .ThenInclude(token => token.SurveyInvitation!)
                    .ThenInclude(invitation => invitation.PatientVisit!)
                        .ThenInclude(visit => visit.Doctor!)
                            .ThenInclude(doctor => doctor.Department)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Doctor>> GetDoctorsForReportsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Doctors
            .GetAllDoctors(trackChanges: false)
            .Include(doctor => doctor.Department)
            .Include(doctor => doctor.Surveys)
            .ToArrayAsync(cancellationToken);
    }
}
