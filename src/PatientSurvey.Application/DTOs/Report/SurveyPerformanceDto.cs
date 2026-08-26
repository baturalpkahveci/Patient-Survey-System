namespace PatientSurvey.Application.DTOs.Report;

public sealed record SurveyPerformanceDto(
    int SurveyId,
    string SurveyTitle,
    string ScopeLabel,
    string? DoctorName,
    string? DepartmentName,
    bool IsActive,
    int QuestionCount,
    int TokenCount,
    int ResponseCount,
    double? AverageScore);
