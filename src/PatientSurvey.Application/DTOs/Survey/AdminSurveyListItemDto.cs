namespace PatientSurvey.Application.DTOs.Survey;

public sealed record AdminSurveyListItemDto(
    int Id,
    string Title,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    int QuestionCount,
    int TokenCount,
    int ResponseCount);
