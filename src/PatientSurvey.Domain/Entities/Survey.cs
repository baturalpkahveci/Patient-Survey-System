namespace PatientSurvey.Domain.Entities;

public sealed class Survey
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DoctorId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Doctor? Doctor { get; set; }
    public Department? Department { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveyAccessToken> AccessTokens { get; set; } = new List<SurveyAccessToken>();
    public ICollection<SurveyInvitation> Invitations { get; set; } = new List<SurveyInvitation>();
}
