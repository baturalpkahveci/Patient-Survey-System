namespace PatientSurvey.Application.DTOs.Report;

public sealed record DepartmentReportDto(
    string DepartmentName,
    int ResponseCount,
    double? AverageScore);
