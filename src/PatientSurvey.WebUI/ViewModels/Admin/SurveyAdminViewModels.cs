using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Survey;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class SurveyIndexViewModel
{
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
}

public sealed class CreateSurveyViewModel
{
    [Required(ErrorMessage = "Anket başlığı zorunludur.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
