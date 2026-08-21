using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Question;

public sealed record SurveyQuestionDto(
    int Id,
    string Text,
    QuestionType Type,
    bool IsRequired,
    int DisplayOrder);
