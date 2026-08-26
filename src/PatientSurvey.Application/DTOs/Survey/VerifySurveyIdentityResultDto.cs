namespace PatientSurvey.Application.DTOs.Survey;

public sealed record VerifySurveyIdentityResultDto(
    string Token,
    int InvitationId,
    string NoticeVersion);
