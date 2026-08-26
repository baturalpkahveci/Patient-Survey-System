namespace PatientSurvey.Domain.Entities;

public sealed class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
