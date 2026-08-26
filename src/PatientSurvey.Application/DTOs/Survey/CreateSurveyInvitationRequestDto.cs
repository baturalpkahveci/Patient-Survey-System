using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.DTOs.Survey;

public sealed record CreateSurveyInvitationRequestDto(
    int SurveyId,
    string PatientFirstName,
    string PatientLastName,
    string TcIdentityNumber,
    string PhoneNumber,
    string Email,
    SurveyDeliveryMethod DeliveryMethod,
    DateTimeOffset? ExpiresAtUtc,
    int CreatedByUserId,
    string SurveyUrlPrefix);
