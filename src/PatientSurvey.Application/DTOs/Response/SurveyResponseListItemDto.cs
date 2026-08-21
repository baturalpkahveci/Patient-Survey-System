namespace PatientSurvey.Application.DTOs.Response;

public sealed record SurveyResponseListItemDto(
    int Id,
    int SurveyId,
    string SurveyTitle,
    string DepartmentName,
    DateTimeOffset SubmittedAtUtc,
    int AnswerCount,
    double? AverageScore);
