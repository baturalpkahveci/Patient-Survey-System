using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Question;

public sealed record CreateQuestionRequestDto(
    int SurveyId,
    string Text,
    QuestionType Type,
    bool IsRequired,
    bool IsActive,
    int DisplayOrder);
