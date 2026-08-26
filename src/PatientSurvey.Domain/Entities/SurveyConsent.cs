namespace PatientSurvey.Domain.Entities;

public sealed class SurveyConsent
{
    public int Id { get; set; }
    public int SurveyInvitationId { get; set; }
    public string NoticeVersion { get; set; } = string.Empty;
    public DateTimeOffset AcceptedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public SurveyInvitation? SurveyInvitation { get; set; }
}
