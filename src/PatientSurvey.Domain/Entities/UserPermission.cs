namespace PatientSurvey.Domain.Entities;

public sealed class UserPermission
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int? GrantedByUserId { get; set; }

    public User? User { get; set; }
    public Permission? Permission { get; set; }
    public User? GrantedByUser { get; set; }
}
