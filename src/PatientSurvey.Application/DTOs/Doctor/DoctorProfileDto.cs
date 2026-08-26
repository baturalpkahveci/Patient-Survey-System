namespace PatientSurvey.Application.DTOs.Doctor;

public sealed record DoctorProfileDto(
    int Id,
    int UserId,
    int DepartmentId,
    string FirstName,
    string LastName,
    string DepartmentName,
    bool IsActive,
    bool DepartmentIsActive);
