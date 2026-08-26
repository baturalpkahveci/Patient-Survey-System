namespace PatientSurvey.Application.DTOs.Doctor;

public sealed record DoctorOptionDto(
    int Id,
    int DepartmentId,
    string DisplayName);
