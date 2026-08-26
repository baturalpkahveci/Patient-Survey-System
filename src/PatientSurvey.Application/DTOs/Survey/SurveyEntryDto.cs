namespace PatientSurvey.Application.DTOs.Survey;

public sealed record SurveyEntryDto(
    string Token,
    int InvitationId,
    string SurveyTitle,
    string? SurveyDescription,
    string KvkkNoticeVersion,
    string KvkkNoticeText);
