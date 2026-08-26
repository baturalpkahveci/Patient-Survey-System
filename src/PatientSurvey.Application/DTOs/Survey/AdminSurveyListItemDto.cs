namespace PatientSurvey.Application.DTOs.Survey;

public sealed record AdminSurveyListItemDto(
    int Id,
    string Title,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    bool IsGeneral,
    int? DepartmentId,
    string? DepartmentName,
    int? DoctorId,
    string? DoctorName,
    int QuestionCount,
    int TokenCount,
    int ResponseCount,
    double? AverageScore);
