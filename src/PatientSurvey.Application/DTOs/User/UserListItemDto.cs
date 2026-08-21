namespace PatientSurvey.Application.DTOs.User;

public sealed record UserListItemDto(
    int Id,
    string Username,
    string RoleName,
    bool IsActive);
