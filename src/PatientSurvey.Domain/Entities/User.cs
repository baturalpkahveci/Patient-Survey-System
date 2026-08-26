namespace PatientSurvey.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Role? Role { get; set; }
    public Doctor? Doctor { get; set; }
    public ICollection<PatientVisit> CreatedPatientVisits { get; set; } = new List<PatientVisit>();
    public ICollection<SurveyInvitation> CreatedSurveyInvitations { get; set; } = new List<SurveyInvitation>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public ICollection<UserPermission> GrantedUserPermissions { get; set; } = new List<UserPermission>();
}
