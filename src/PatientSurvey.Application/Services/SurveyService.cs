using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;
using SurveyEntity = PatientSurvey.Domain.Entities.Survey;

namespace PatientSurvey.Application.Services;

public sealed class SurveyService
{
    private readonly ISurveyReadRepository _repository;
    private readonly IAdminSurveyRepository _adminRepository;
    private readonly IClock _clock;

    public SurveyService(
        ISurveyReadRepository repository,
        IAdminSurveyRepository adminRepository,
        IClock clock)
    {
        _repository = repository;
        _adminRepository = adminRepository;
        _clock = clock;
    }

    public async Task<ServiceResult<SurveyFormDto>> GetSurveyFormAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Failure("invalid_token", "Bu anket baglantisi gecersiz veya artik kullanilamiyor.");
        }

        var accessToken = await _repository.GetTokenWithActiveSurveyAsync(token.Trim(), cancellationToken);
        if (accessToken?.Survey is null || accessToken.UsedAtUtc.HasValue)
        {
            return Failure("invalid_token", "Bu anket baglantisi gecersiz veya artik kullanilamiyor.");
        }

        if (accessToken.ExpiresAtUtc.HasValue && accessToken.ExpiresAtUtc.Value <= _clock.UtcNow)
        {
            return Failure("expired_token", "Bu anket baglantisinin kullanim suresi dolmus.");
        }

        var departments = await _repository.GetActiveDepartmentsAsync(cancellationToken);
        var questions = accessToken.Survey.Questions
            .Where(question => question.IsActive)
            .OrderBy(question => question.DisplayOrder)
            .Select(question => new SurveyQuestionDto(
                question.Id,
                question.Text,
                question.Type,
                question.IsRequired,
                question.DisplayOrder))
            .ToArray();

        var dto = new SurveyFormDto(
            accessToken.Token,
            accessToken.Survey.Id,
            accessToken.Survey.Title,
            accessToken.Survey.Description,
            questions,
            departments.Select(department => new DepartmentDto(department.Id, department.Name)).ToArray());

        return ServiceResult<SurveyFormDto>.Success(dto);
    }

    public async Task<IReadOnlyCollection<AdminSurveyListItemDto>> GetAdminSurveysAsync(
        CancellationToken cancellationToken = default)
    {
        var surveys = await _adminRepository.GetAllSurveysWithQuestionsAsync(cancellationToken);
        return surveys
            .OrderBy(survey => survey.Title)
            .Select(survey => new AdminSurveyListItemDto(
                survey.Id,
                survey.Title,
                survey.Description,
                survey.IsActive,
                survey.CreatedAtUtc,
                survey.Questions.Count,
                survey.AccessTokens.Count,
                survey.AccessTokens.Count(token => token.SurveyResponse is not null)))
            .ToArray();
    }

    public async Task<ServiceResult<int>> CreateSurveyAsync(
        CreateSurveyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return ServiceResult<int>.Failure("title_required", "Anket basligi zorunludur.");
        }

        var survey = new SurveyEntity
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = _clock.UtcNow
        };

        _adminRepository.AddSurvey(survey);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(survey.Id);
    }

    public async Task<ServiceResult> ToggleSurveyStatusAsync(
        int surveyId,
        CancellationToken cancellationToken = default)
    {
        var survey = await _adminRepository.GetSurveyByIdAsync(surveyId, cancellationToken);
        if (survey is null)
        {
            return ServiceResult.Failure("survey_not_found", "Anket bulunamadi.");
        }

        survey.IsActive = !survey.IsActive;
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static ServiceResult<SurveyFormDto> Failure(string code, string message)
    {
        return ServiceResult<SurveyFormDto>.Failure(code, message);
    }
}
