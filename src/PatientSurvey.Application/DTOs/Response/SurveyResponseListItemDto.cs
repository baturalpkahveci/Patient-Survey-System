namespace PatientSurvey.Application.DTOs.Response;

public sealed record SurveyResponseListItemDto(
    int Id,
    int SurveyId,
    string SurveyTitle,
    string DepartmentName,
    DateTimeOffset SubmittedAtUtc,
    int AnswerCount,
    double? AverageScore,
    bool IsGeneralSurvey = true,
    int? SurveyDoctorId = null,
    string PatientName = "Anonim",
    string? PatientPhone = null,
    string? PatientEmail = null,
    int? InvitationId = null,
    DateTimeOffset? ExaminedAtUtc = null,
    IReadOnlyCollection<SurveyResponseAnswerDto>? Answers = null);
