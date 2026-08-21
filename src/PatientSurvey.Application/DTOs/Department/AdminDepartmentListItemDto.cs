namespace PatientSurvey.Application.DTOs.Department;

public sealed record AdminDepartmentListItemDto(
    int Id,
    string Name,
    bool IsActive,
    int ResponseCount);
