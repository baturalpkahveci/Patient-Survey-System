namespace PatientSurvey.Application.DTOs.Department;

public sealed record UpdateDepartmentRequestDto(
    int Id,
    string Name);
