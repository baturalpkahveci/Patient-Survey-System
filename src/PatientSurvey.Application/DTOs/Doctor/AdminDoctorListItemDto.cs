namespace PatientSurvey.Application.DTOs.Doctor;

public sealed record AdminDoctorListItemDto(
    int Id,
    int UserId,
    string Username,
    string FirstName,
    string LastName,
    int DepartmentId,
    string DepartmentName,
    bool IsActive);
