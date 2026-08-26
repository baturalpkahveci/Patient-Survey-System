using PatientSurvey.Application.DTOs.Response;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class ResultIndexViewModel
{
    public IReadOnlyCollection<SurveyResponseListItemDto> Results { get; set; } = Array.Empty<SurveyResponseListItemDto>();
    public IReadOnlyCollection<FilterOptionViewModel> SurveyOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> DepartmentOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public int? SurveyId { get; set; }
    public string? DepartmentName { get; set; }
    public string? PatientName { get; set; }
    public string? SurveyScope { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public double? MinAverage { get; set; }
    public int TotalCount { get; set; }
}

public sealed class ResultDetailViewModel
{
    public SurveyResponseDetailDto? Result { get; set; }
}
