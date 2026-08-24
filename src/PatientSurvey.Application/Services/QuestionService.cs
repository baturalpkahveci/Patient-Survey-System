using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Services;

public sealed class QuestionService
{
    private readonly IAdminQuestionRepository _repository;

    public QuestionService(IAdminQuestionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<AdminQuestionListItemDto>> GetAdminQuestionsAsync(
        CancellationToken cancellationToken = default)
    {
        var questions = await _repository.GetAllQuestionsWithSurveysAsync(cancellationToken);
        return questions
            .OrderBy(question => question.Survey?.Title)
            .ThenBy(question => question.DisplayOrder)
            .Select(question => new AdminQuestionListItemDto(
                question.Id,
                question.SurveyId,
                question.Survey?.Title ?? string.Empty,
                question.Text,
                question.Type,
                question.IsRequired,
                question.IsActive,
                question.DisplayOrder))
            .ToArray();
    }

    public async Task<ServiceResult<int>> CreateQuestionAsync(
        CreateQuestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var text = request.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return ServiceResult<int>.Failure("text_required", "Soru metni zorunludur.");
        }

        var survey = await _repository.GetSurveyByIdAsync(request.SurveyId, cancellationToken);
        if (survey is null)
        {
            return ServiceResult<int>.Failure("survey_not_found", "Anket bulunamadı.");
        }

        var question = new Question
        {
            SurveyId = survey.Id,
            Text = text,
            Type = request.Type,
            IsRequired = request.IsRequired,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _repository.AddQuestion(question);
        await _repository.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(question.Id);
    }

    public async Task<ServiceResult> ToggleQuestionStatusAsync(
        int questionId,
        CancellationToken cancellationToken = default)
    {
        var question = await _repository.GetQuestionByIdAsync(questionId, cancellationToken);
        if (question is null)
        {
            return ServiceResult.Failure("question_not_found", "Soru bulunamadı.");
        }

        question.IsActive = !question.IsActive;
        await _repository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}
