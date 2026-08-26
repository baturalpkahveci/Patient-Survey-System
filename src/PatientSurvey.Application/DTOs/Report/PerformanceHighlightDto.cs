namespace PatientSurvey.Application.DTOs.Report;

public sealed record PerformanceHighlightDto(
    string Name,
    string Context,
    int ResponseCount,
    double? AverageScore);
