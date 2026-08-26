namespace PatientSurvey.Domain.Entities;

public sealed class AuditLog
{
    public int Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "Sistem";
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? ChangesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestPath { get; set; }

    public User? User { get; set; }
}
