using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Survey;

public sealed record CreatedSurveyInvitationDto(
    int SurveyId,
    int InvitationId,
    int PatientReference,
    string Token,
    SurveyDeliveryMethod DeliveryMethod,
    SurveyDeliveryStatus DeliveryStatus,
    string Message);
