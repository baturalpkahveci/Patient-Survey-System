namespace PatientSurvey.Application.DTOs.Report;

public sealed record SurveyReportDto(
    int SurveyId,
    string SurveyTitle,
    bool IsActive,
    int QuestionCount,
    int ResponseCount,
    int TokenCount,
    double? AverageScore,
    IReadOnlyCollection<DepartmentReportDto> Departments);
