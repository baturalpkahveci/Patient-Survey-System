namespace PatientSurvey.Application.DTOs.User;

public sealed record CreateUserRequestDto(
    string Username,
    string Password,
    int RoleId,
    bool IsActive);
