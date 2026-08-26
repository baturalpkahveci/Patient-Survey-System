namespace PatientSurvey.Application.DTOs.User;

public sealed record UserListItemDto(
    int Id,
    string Username,
    string RoleName,
    bool IsActive,
    int? DoctorId = null,
    string? DoctorFirstName = null,
    string? DoctorLastName = null,
    int? DoctorDepartmentId = null,
    string? DoctorDepartmentName = null,
    bool? DoctorIsActive = null,
    bool CanViewPatientPersonalData = false);
