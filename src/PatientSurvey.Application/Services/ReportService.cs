using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Report;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Application.Services;

public sealed class ReportService
{
    private readonly IManagementReportRepository _repository;
    private readonly PermissionService? _permissionService;

    public ReportService(IManagementReportRepository repository, PermissionService? permissionService = null)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken = default)
    {
        var surveys = await _repository.GetSurveysForDashboardAsync(cancellationToken);
        var tokens = surveys.SelectMany(survey => survey.AccessTokens).ToArray();
        var responses = tokens
            .Where(token => token.SurveyResponse is not null)
            .Select(token => token.SurveyResponse!)
            .ToArray();

        return new DashboardOverviewDto(
            surveys.Count,
            surveys.Count(survey => survey.IsActive),
            surveys.Sum(survey => survey.Questions.Count),
            responses.Length,
            tokens.Length,
            tokens.Count(token => token.UsedAtUtc is null && token.SurveyResponse is null));
    }

    public async Task<IReadOnlyCollection<SurveyResponseListItemDto>> GetResultsAsync(
        CancellationToken cancellationToken = default)
    {
        var canViewPatientPersonalData = _permissionService is not null
            && await _permissionService.CanCurrentUserViewPatientPersonalDataAsync("Anket sonuçları", cancellationToken);
        var responses = await _repository.GetResponsesForResultsAsync(canViewPatientPersonalData, cancellationToken);
        return responses
            .OrderByDescending(response => response.SubmittedAtUtc)
            .Select(response => ToListItem(response, canViewPatientPersonalData))
            .ToArray();
    }

    public async Task<ServiceResult<SurveyResponseDetailDto>> GetResultDetailAsync(
        int responseId,
        CancellationToken cancellationToken = default)
    {
        var canViewPatientPersonalData = _permissionService is not null
            && await _permissionService.CanCurrentUserViewPatientPersonalDataAsync("Anket sonucu detayı", cancellationToken);
        var response = await _repository.GetResponseDetailAsync(responseId, canViewPatientPersonalData, cancellationToken);
        if (response?.Token?.Survey is null || response.Department is null)
        {
            return ServiceResult<SurveyResponseDetailDto>.Failure("response_not_found", "Anket sonucu bulunamadı.");
        }

        var answers = ToAnswerDtos(response);
        var scoreAnswers = response.Answers
            .Where(answer => answer.ScoreValue.HasValue)
            .Select(answer => answer.ScoreValue!.Value)
            .ToArray();
        var patient = response.Token.SurveyInvitation?.PatientVisit?.Patient;
        var visit = response.Token.SurveyInvitation?.PatientVisit;

        return ServiceResult<SurveyResponseDetailDto>.Success(new SurveyResponseDetailDto(
            response.Id,
            response.Token.Survey.Title,
            response.Department.Name,
            response.SubmittedAtUtc,
            answers,
            canViewPatientPersonalData ? FormatPatientName(patient, visit?.PatientId ?? 0) : FormatPatientReference(visit?.PatientId ?? 0),
            canViewPatientPersonalData ? NormalizePatientInfo(patient?.PhoneNumber) : null,
            canViewPatientPersonalData ? NormalizePatientInfo(patient?.Email) : null,
            response.Token.SurveyInvitationId,
            visit?.ExaminedAtUtc,
            scoreAnswers.Length == 0 ? null : Math.Round(scoreAnswers.Average(), 2)));
    }

    public async Task<IReadOnlyCollection<SurveyReportDto>> GetSurveyReportsAsync(
        CancellationToken cancellationToken = default)
    {
        var surveys = await _repository.GetSurveysForReportsAsync(cancellationToken);
        return surveys
            .OrderBy(survey => survey.Title)
            .Select(survey =>
            {
                var responses = survey.AccessTokens
                    .Where(token => token.SurveyResponse is not null)
                    .Select(token => token.SurveyResponse!)
                    .ToArray();
                var scoreAnswers = responses
                    .SelectMany(response => response.Answers)
                    .Where(answer => answer.ScoreValue.HasValue)
                    .ToArray();

                var departments = responses
                    .Where(response => response.Department is not null)
                    .GroupBy(response => response.Department!.Name)
                    .OrderBy(group => group.Key)
                    .Select(group =>
                    {
                        var departmentScores = group
                            .SelectMany(response => response.Answers)
                            .Where(answer => answer.ScoreValue.HasValue)
                            .Select(answer => answer.ScoreValue!.Value)
                            .ToArray();

                        return new DepartmentReportDto(
                            group.Key,
                            group.Count(),
                            departmentScores.Length == 0 ? null : Math.Round(departmentScores.Average(), 2));
                    })
                    .ToArray();

                return new SurveyReportDto(
                    survey.Id,
                    survey.Title,
                    survey.IsActive,
                    survey.Questions.Count,
                    responses.Length,
                    survey.AccessTokens.Count,
                    scoreAnswers.Length == 0 ? null : Math.Round(scoreAnswers.Average(answer => answer.ScoreValue!.Value), 2),
                    departments);
            })
            .ToArray();
    }

    public async Task<ManagerReportDashboardDto> GetManagerReportDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var surveys = await _repository.GetSurveysForReportsAsync(cancellationToken);
        var allDoctors = await _repository.GetDoctorsForReportsAsync(cancellationToken);
        var surveyPerformances = surveys
            .OrderBy(survey => survey.Title)
            .Select(ToSurveyPerformance)
            .ToArray();

        var responses = surveys
            .SelectMany(survey => survey.AccessTokens)
            .Where(token => token.SurveyResponse is not null)
            .Select(token => token.SurveyResponse!)
            .ToArray();

        var responsesByDoctor = responses
            .Select(response => new
            {
                Doctor = ResolveDoctor(response),
                Response = response
            })
            .Where(item => item.Doctor is not null)
            .GroupBy(item => item.Doctor!.Id)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Response).ToArray());

        var doctors = allDoctors
            .OrderBy(doctor => doctor.LastName)
            .ThenBy(doctor => doctor.FirstName)
            .Select(doctor =>
            {
                responsesByDoctor.TryGetValue(doctor.Id, out var doctorResponses);
                doctorResponses ??= Array.Empty<PatientSurvey.Domain.Entities.SurveyResponse>();
                var surveyCount = doctorResponses
                    .Select(response => response.Token?.SurveyId)
                    .Where(surveyId => surveyId.HasValue)
                    .Distinct()
                    .Count()
                    + doctor.Surveys.Count(survey => doctorResponses.All(response => response.Token?.SurveyId != survey.Id));

                return new DoctorPerformanceDto(
                    doctor.Id,
                    $"Dr. {doctor.FirstName} {doctor.LastName}",
                    doctor.Department?.Name ?? "Bölüm yok",
                    surveyCount,
                    doctorResponses.Length,
                    AverageScore(doctorResponses.SelectMany(response => response.Answers)));
            })
            .OrderByDescending(doctor => doctor.AverageScore.HasValue)
            .ThenByDescending(doctor => doctor.AverageScore)
            .ThenByDescending(doctor => doctor.ResponseCount)
            .ThenBy(doctor => doctor.DoctorName)
            .ToArray();

        var topDoctors = doctors
            .Where(doctor => doctor.AverageScore.HasValue)
            .OrderByDescending(doctor => doctor.AverageScore)
            .ThenByDescending(doctor => doctor.ResponseCount)
            .Take(5)
            .Select(doctor => new PerformanceHighlightDto(doctor.DoctorName, doctor.DepartmentName, doctor.ResponseCount, doctor.AverageScore))
            .ToArray();

        var lowDoctors = doctors
            .Where(doctor => doctor.AverageScore.HasValue)
            .OrderBy(doctor => doctor.AverageScore)
            .ThenByDescending(doctor => doctor.ResponseCount)
            .Take(5)
            .Select(doctor => new PerformanceHighlightDto(doctor.DoctorName, doctor.DepartmentName, doctor.ResponseCount, doctor.AverageScore))
            .ToArray();

        var scoredSurveys = surveyPerformances.Where(survey => survey.AverageScore.HasValue).ToArray();
        var topSurveys = scoredSurveys
            .OrderByDescending(survey => survey.AverageScore)
            .ThenByDescending(survey => survey.ResponseCount)
            .Take(5)
            .Select(survey => new PerformanceHighlightDto(survey.SurveyTitle, survey.ScopeLabel, survey.ResponseCount, survey.AverageScore))
            .ToArray();

        var lowSurveys = scoredSurveys
            .OrderBy(survey => survey.AverageScore)
            .ThenByDescending(survey => survey.ResponseCount)
            .Take(5)
            .Select(survey => new PerformanceHighlightDto(survey.SurveyTitle, survey.ScopeLabel, survey.ResponseCount, survey.AverageScore))
            .ToArray();

        return new ManagerReportDashboardDto(
            surveys.Count,
            doctors.Length,
            responses.Length,
            AverageScore(responses.SelectMany(response => response.Answers)),
            doctors,
            surveyPerformances,
            topDoctors,
            lowDoctors,
            topSurveys,
            lowSurveys);
    }

    private static SurveyResponseListItemDto ToListItem(
        PatientSurvey.Domain.Entities.SurveyResponse response,
        bool includePatientPersonalData)
    {
        var scoreAnswers = response.Answers
            .Where(answer => answer.ScoreValue.HasValue)
            .Select(answer => answer.ScoreValue!.Value)
            .ToArray();
        var patient = response.Token?.SurveyInvitation?.PatientVisit?.Patient;
        var visit = response.Token?.SurveyInvitation?.PatientVisit;

        return new SurveyResponseListItemDto(
            response.Id,
            response.Token?.SurveyId ?? 0,
            response.Token?.Survey?.Title ?? string.Empty,
            response.Department?.Name ?? string.Empty,
            response.SubmittedAtUtc,
            response.Answers.Count,
            scoreAnswers.Length == 0 ? null : Math.Round(scoreAnswers.Average(), 2),
            response.Token?.Survey?.DoctorId is null && response.Token?.Survey?.DepartmentId is null,
            response.Token?.Survey?.DoctorId,
            includePatientPersonalData ? FormatPatientName(patient, visit?.PatientId ?? 0) : FormatPatientReference(visit?.PatientId ?? 0),
            includePatientPersonalData ? NormalizePatientInfo(patient?.PhoneNumber) : null,
            includePatientPersonalData ? NormalizePatientInfo(patient?.Email) : null,
            response.Token?.SurveyInvitationId,
            visit?.ExaminedAtUtc,
            ToAnswerDtos(response));
    }

    private static SurveyPerformanceDto ToSurveyPerformance(PatientSurvey.Domain.Entities.Survey survey)
    {
        var responses = survey.AccessTokens
            .Where(token => token.SurveyResponse is not null)
            .Select(token => token.SurveyResponse!)
            .ToArray();

        var scopeLabel = survey.DoctorId.HasValue || survey.DepartmentId.HasValue
            ? "Hedefli"
            : "Genel";

        return new SurveyPerformanceDto(
            survey.Id,
            survey.Title,
            scopeLabel,
            survey.Doctor is null ? null : $"Dr. {survey.Doctor.FirstName} {survey.Doctor.LastName}",
            survey.Department?.Name,
            survey.IsActive,
            survey.Questions.Count,
            survey.AccessTokens.Count,
            responses.Length,
            AverageScore(responses.SelectMany(response => response.Answers)));
    }

    private static PatientSurvey.Domain.Entities.Doctor? ResolveDoctor(PatientSurvey.Domain.Entities.SurveyResponse response)
    {
        return response.Token?.Survey?.Doctor
            ?? response.Token?.SurveyInvitation?.PatientVisit?.Doctor;
    }

    private static double? AverageScore(IEnumerable<PatientSurvey.Domain.Entities.Answer> answers)
    {
        var scores = answers
            .Where(answer => answer.ScoreValue.HasValue)
            .Select(answer => answer.ScoreValue!.Value)
            .ToArray();

        return scores.Length == 0 ? null : Math.Round(scores.Average(), 2);
    }

    private static SurveyResponseAnswerDto[] ToAnswerDtos(PatientSurvey.Domain.Entities.SurveyResponse response)
    {
        return response.Answers
            .OrderBy(answer => answer.Question?.DisplayOrder ?? 0)
            .Select(answer => new SurveyResponseAnswerDto(
                answer.Question?.Text ?? string.Empty,
                answer.Question?.Type ?? default,
                answer.Question?.DisplayOrder ?? 0,
                answer.ScoreValue,
                answer.TextValue,
                answer.BooleanValue))
            .ToArray();
    }

    private static string FormatPatientName(PatientSurvey.Domain.Entities.Patient? patient, int patientId)
    {
        if (patient is null)
        {
            return FormatPatientReference(patientId);
        }

        var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? FormatPatientReference(patientId) : fullName;
    }

    private static string FormatPatientReference(int patientId)
    {
        return patientId > 0 ? $"Hasta #{patientId}" : "Anonim";
    }

    private static string? NormalizePatientInfo(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
