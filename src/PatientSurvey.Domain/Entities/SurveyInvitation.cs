using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Domain.Entities;

public sealed class SurveyInvitation
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public int PatientVisitId { get; set; }
    public int CreatedByUserId { get; set; }
    public SurveyDeliveryMethod DeliveryMethod { get; set; }
    public SurveyDeliveryStatus DeliveryStatus { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAtUtc { get; set; }

    public Survey? Survey { get; set; }
    public PatientVisit? PatientVisit { get; set; }
    public User? CreatedByUser { get; set; }
    public SurveyAccessToken? AccessToken { get; set; }
    public SurveyConsent? Consent { get; set; }
}
