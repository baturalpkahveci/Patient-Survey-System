namespace PatientSurvey.Application.DTOs.Response;

public sealed record SubmitSurveyResultDto(int SurveyResponseId, DateTimeOffset SubmittedAtUtc);
