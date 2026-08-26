namespace PatientSurvey.Domain.Entities;

public sealed class SurveyAccessToken
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public int? SurveyInvitationId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }

    public Survey? Survey { get; set; }
    public SurveyInvitation? SurveyInvitation { get; set; }
    public SurveyResponse? SurveyResponse { get; set; }
}
