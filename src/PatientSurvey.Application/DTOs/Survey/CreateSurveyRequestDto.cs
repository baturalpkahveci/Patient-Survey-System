namespace PatientSurvey.Application.DTOs.Survey;

public sealed record CreateSurveyRequestDto(
    string Title,
    string? Description,
    bool IsActive,
    bool IsGeneral = true,
    int? DepartmentId = null,
    int? DoctorId = null,
    int? CreatedByUserId = null,
    string? CreatedByRole = null);
