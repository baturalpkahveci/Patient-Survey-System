namespace PatientSurvey.Application.DTOs.Survey;

public sealed record SurveyAccessTokenListItemDto(
    int Id,
    int SurveyId,
    string SurveyTitle,
    string Token,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? UsedAtUtc,
    bool HasResponse);
