using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Question;

public sealed record AdminQuestionListItemDto(
    int Id,
    int SurveyId,
    string SurveyTitle,
    string Text,
    QuestionType Type,
    bool IsRequired,
    bool IsActive,
    int DisplayOrder);
