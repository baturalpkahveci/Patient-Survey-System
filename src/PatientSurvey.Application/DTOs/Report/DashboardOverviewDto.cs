namespace PatientSurvey.Application.DTOs.Report;

public sealed record DashboardOverviewDto(
    int SurveyCount,
    int ActiveSurveyCount,
    int QuestionCount,
    int ResponseCount,
    int TokenCount,
    int UnusedTokenCount);
