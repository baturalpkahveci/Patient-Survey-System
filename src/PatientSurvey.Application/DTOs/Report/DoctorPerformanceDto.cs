namespace PatientSurvey.Application.DTOs.Report;

public sealed record DoctorPerformanceDto(
    int DoctorId,
    string DoctorName,
    string DepartmentName,
    int SurveyCount,
    int ResponseCount,
    double? AverageScore);
