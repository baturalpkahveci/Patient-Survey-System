namespace PatientSurvey.Application.DTOs.Doctor;

public sealed record UpdateDoctorDepartmentRequestDto(
    int DoctorId,
    int DepartmentId);
