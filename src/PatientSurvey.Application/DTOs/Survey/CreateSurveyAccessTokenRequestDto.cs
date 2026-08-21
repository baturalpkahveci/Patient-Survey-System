namespace PatientSurvey.Application.DTOs.Survey;

public sealed record CreateSurveyAccessTokenRequestDto(
    int SurveyId,
    DateTimeOffset? ExpiresAtUtc);
