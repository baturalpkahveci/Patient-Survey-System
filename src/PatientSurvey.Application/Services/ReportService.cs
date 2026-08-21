using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Report;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Application.Services;

public sealed class ReportService
{
    private readonly IManagementReportRepository _repository;

    public ReportService(IManagementReportRepository repository)
    {
        _repository = repository;
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
        var responses = await _repository.GetResponsesForResultsAsync(cancellationToken);
        return responses
            .OrderByDescending(response => response.SubmittedAtUtc)
            .Select(ToListItem)
            .ToArray();
    }

    public async Task<ServiceResult<SurveyResponseDetailDto>> GetResultDetailAsync(
        int responseId,
        CancellationToken cancellationToken = default)
    {
        var response = await _repository.GetResponseDetailAsync(responseId, cancellationToken);
        if (response?.Token?.Survey is null || response.Department is null)
        {
            return ServiceResult<SurveyResponseDetailDto>.Failure("response_not_found", "Anket sonucu bulunamadi.");
        }

        var answers = response.Answers
            .OrderBy(answer => answer.Question?.DisplayOrder ?? 0)
            .Select(answer => new SurveyResponseAnswerDto(
                answer.Question?.Text ?? string.Empty,
                answer.Question!.Type,
                answer.Question.DisplayOrder,
                answer.ScoreValue,
                answer.TextValue,
                answer.BooleanValue))
            .ToArray();

        return ServiceResult<SurveyResponseDetailDto>.Success(new SurveyResponseDetailDto(
            response.Id,
            response.Token.Survey.Title,
            response.Department.Name,
            response.SubmittedAtUtc,
            answers));
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

    private static SurveyResponseListItemDto ToListItem(PatientSurvey.Domain.Entities.SurveyResponse response)
    {
        var scoreAnswers = response.Answers
            .Where(answer => answer.ScoreValue.HasValue)
            .Select(answer => answer.ScoreValue!.Value)
            .ToArray();

        return new SurveyResponseListItemDto(
            response.Id,
            response.Token?.SurveyId ?? 0,
            response.Token?.Survey?.Title ?? string.Empty,
            response.Department?.Name ?? string.Empty,
            response.SubmittedAtUtc,
            response.Answers.Count,
            scoreAnswers.Length == 0 ? null : Math.Round(scoreAnswers.Average(), 2));
    }
}
