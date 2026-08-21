namespace PatientSurvey.Application.DTOs.Report;

public sealed record SurveySummaryDto(int SurveyId, string SurveyTitle, int ResponseCount);
