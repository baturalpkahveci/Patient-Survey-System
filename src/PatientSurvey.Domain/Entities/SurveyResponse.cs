namespace PatientSurvey.Domain.Entities;

public sealed class SurveyResponse
{
    public int Id { get; set; }
    public int TokenId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public SurveyAccessToken? Token { get; set; }
    public Department? Department { get; set; }
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
