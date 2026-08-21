namespace PatientSurvey.Application.DTOs.Response;

public sealed record SurveyResponseDetailDto(
    int Id,
    string SurveyTitle,
    string DepartmentName,
    DateTimeOffset SubmittedAtUtc,
    IReadOnlyCollection<SurveyResponseAnswerDto> Answers);
