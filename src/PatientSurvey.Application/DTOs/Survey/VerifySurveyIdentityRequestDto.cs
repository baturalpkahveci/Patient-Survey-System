namespace PatientSurvey.Application.DTOs.Survey;

public sealed record VerifySurveyIdentityRequestDto(
    string Token,
    string TcIdentityNumber,
    bool KvkkAccepted);
