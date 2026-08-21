namespace PatientSurvey.Application.DTOs.Response;

public sealed record SubmitAnswerDto(
    int QuestionId,
    int? ScoreValue,
    string? TextValue,
    bool? BooleanValue);
