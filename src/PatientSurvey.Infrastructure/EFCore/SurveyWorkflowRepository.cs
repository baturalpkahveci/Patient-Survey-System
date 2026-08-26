using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using AppIAppTransaction = PatientSurvey.Application.Interfaces.IAppTransaction;
using AppISurveyAccessTokenRepository = PatientSurvey.Application.Interfaces.ISurveyAccessTokenRepository;
using AppISurveyInvitationRepository = PatientSurvey.Application.Interfaces.ISurveyInvitationRepository;
using AppISurveyReadRepository = PatientSurvey.Application.Interfaces.ISurveyReadRepository;
using AppISurveySubmissionRepository = PatientSurvey.Application.Interfaces.ISurveySubmissionRepository;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class SurveyWorkflowRepository :
    AppISurveySubmissionRepository,
    AppISurveyReadRepository,
    AppISurveyAccessTokenRepository,
    AppISurveyInvitationRepository
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
            .Include(accessToken => accessToken.SurveyInvitation!)
                .ThenInclude(invitation => invitation.PatientVisit!)
                    .ThenInclude(visit => visit.Patient)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Surveys.GetOneSurveyByIdAsync(surveyId, trackChanges: false, cancellationToken);
    }

    public Task<Survey?> GetSurveyByIdAsync(int surveyId, bool trackChanges, CancellationToken cancellationToken)
    {
        return _repositoryManager.Surveys.GetOneSurveyByIdAsync(surveyId, trackChanges, cancellationToken);
    }

    public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Doctors.GetOneDoctorByIdAsync(doctorId, trackChanges: false, cancellationToken);
    }

    public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Doctors.GetOneDoctorByUserIdAsync(userId, trackChanges: false, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Doctor>> GetActiveDoctorsByDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        return await _repositoryManager.Doctors
            .GetAllDoctors(trackChanges: false)
            .Where(doctor => doctor.DepartmentId == departmentId && doctor.IsActive && doctor.Department != null && doctor.Department.IsActive)
            .ToArrayAsync(cancellationToken);
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

    public void AddSurveyConsent(SurveyConsent consent)
    {
        _repositoryManager.SurveyConsents.CreateOneSurveyConsent(consent);
    }

    public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken)
    {
        return _repositoryManager.SurveyAccessTokens.TokenExistsAsync(token, cancellationToken);
    }

    public void AddToken(SurveyAccessToken token)
    {
        _repositoryManager.SurveyAccessTokens.CreateOneSurveyAccessToken(token);
    }

    public Task<Patient?> GetPatientByTcHashAsync(string tcIdentityLookupHash, CancellationToken cancellationToken)
    {
        return _repositoryManager.Patients.GetOnePatientByTcHashAsync(
            tcIdentityLookupHash,
            trackChanges: true,
            cancellationToken);
    }

    public void AddPatient(Patient patient)
    {
        _repositoryManager.Patients.CreateOnePatient(patient);
    }

    public void AddPatientVisit(PatientVisit visit)
    {
        _repositoryManager.PatientVisits.CreateOnePatientVisit(visit);
    }

    public void AddSurveyInvitation(SurveyInvitation invitation)
    {
        _repositoryManager.SurveyInvitations.CreateOneSurveyInvitation(invitation);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
