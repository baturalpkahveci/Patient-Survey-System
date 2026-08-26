namespace PatientSurvey.Domain.Entities;

public sealed class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public ICollection<PatientVisit> PatientVisits { get; set; } = new List<PatientVisit>();
    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
