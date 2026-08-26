namespace PatientSurvey.Application.DTOs.User;

public sealed record AuthenticatedUserDto(
    int Id,
    string Username,
    string RoleName,
    IReadOnlyCollection<string>? PermissionNames = null);
