namespace PatientSurvey.Application.DTOs.Response;

public sealed record SurveyResponseDetailDto(
    int Id,
    string SurveyTitle,
    string DepartmentName,
    DateTimeOffset SubmittedAtUtc,
    IReadOnlyCollection<SurveyResponseAnswerDto> Answers,
    string PatientName = "Anonim",
    string? PatientPhone = null,
    string? PatientEmail = null,
    int? InvitationId = null,
    DateTimeOffset? ExaminedAtUtc = null,
    double? AverageScore = null);
