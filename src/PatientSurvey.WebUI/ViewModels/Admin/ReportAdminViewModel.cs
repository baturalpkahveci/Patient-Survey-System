using PatientSurvey.Application.DTOs.Report;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class ReportIndexViewModel
{
    public IReadOnlyCollection<SurveyReportDto> Reports { get; set; } = Array.Empty<SurveyReportDto>();
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? MinResponses { get; set; }
    public int TotalCount { get; set; }
}
