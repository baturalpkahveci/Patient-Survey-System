namespace PatientSurvey.Application.DTOs.Report;

public sealed record ManagerReportDashboardDto(
    int SurveyCount,
    int DoctorCount,
    int ResponseCount,
    double? OverallAverageScore,
    IReadOnlyCollection<DoctorPerformanceDto> Doctors,
    IReadOnlyCollection<SurveyPerformanceDto> Surveys,
    IReadOnlyCollection<PerformanceHighlightDto> TopDoctors,
    IReadOnlyCollection<PerformanceHighlightDto> LowDoctors,
    IReadOnlyCollection<PerformanceHighlightDto> TopSurveys,
    IReadOnlyCollection<PerformanceHighlightDto> LowSurveys);
