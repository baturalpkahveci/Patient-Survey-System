namespace PatientSurvey.Application.DTOs.Survey;

public sealed record CreateSurveyRequestDto(
    string Title,
    string? Description,
    bool IsActive);
