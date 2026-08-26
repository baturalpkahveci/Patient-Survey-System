namespace PatientSurvey.Application.DTOs.Doctor;

public sealed record UpsertDoctorProfileRequestDto(
    int UserId,
    string FirstName,
    string LastName,
    int DepartmentId);
