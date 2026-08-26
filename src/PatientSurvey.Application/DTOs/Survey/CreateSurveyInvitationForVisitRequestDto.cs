using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Survey;

public sealed record CreateSurveyInvitationForVisitRequestDto(
    int SurveyId,
    int PatientVisitId,
    SurveyDeliveryMethod DeliveryMethod,
    DateTimeOffset? ExpiresAtUtc,
    int CreatedByUserId,
    string SurveyUrlPrefix);
