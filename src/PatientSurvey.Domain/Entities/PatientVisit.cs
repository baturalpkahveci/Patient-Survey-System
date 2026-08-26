namespace PatientSurvey.Domain.Entities;

public sealed class PatientVisit
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? DoctorId { get; set; }
    public int? DepartmentId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTimeOffset ExaminedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public Department? Department { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<SurveyInvitation> Invitations { get; set; } = new List<SurveyInvitation>();
}
