namespace PatientSurvey.Domain.Entities;

public sealed class Doctor
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DepartmentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public Department? Department { get; set; }
    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    public ICollection<PatientVisit> PatientVisits { get; set; } = new List<PatientVisit>();
}
