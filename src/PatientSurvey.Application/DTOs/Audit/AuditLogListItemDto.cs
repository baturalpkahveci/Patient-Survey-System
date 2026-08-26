namespace PatientSurvey.Application.DTOs.Audit;

public sealed record AuditLogListItemDto(
    int Id,
    DateTimeOffset OccurredAtUtc,
    int? UserId,
    string Username,
    string? UserRole,
    string Action,
    string EntityName,
    string? EntityId,
    string Summary,
    string? ChangesJson,
    string? IpAddress,
    string? RequestPath);
