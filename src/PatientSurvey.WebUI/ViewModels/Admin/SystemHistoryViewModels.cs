using PatientSurvey.Application.DTOs.Audit;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class SystemHistoryIndexViewModel
{
    public IReadOnlyCollection<AuditLogListItemDto> Logs { get; set; } = Array.Empty<AuditLogListItemDto>();
    public IReadOnlyCollection<FilterOptionViewModel> UserOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> ActionOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> EntityOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public string? Search { get; set; }
    public string? Username { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortDirection { get; set; } = "desc";
    public int TotalCount { get; set; }
}
