using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Response;

public sealed record SurveyResponseAnswerDto(
    string QuestionText,
    QuestionType QuestionType,
    int DisplayOrder,
    int? ScoreValue,
    string? TextValue,
    bool? BooleanValue);
