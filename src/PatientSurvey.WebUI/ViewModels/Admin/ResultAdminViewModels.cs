using PatientSurvey.Application.DTOs.Response;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class ResultIndexViewModel
{
    public IReadOnlyCollection<SurveyResponseListItemDto> Results { get; set; } = Array.Empty<SurveyResponseListItemDto>();
}

public sealed class ResultDetailViewModel
{
    public SurveyResponseDetailDto? Result { get; set; }
}
