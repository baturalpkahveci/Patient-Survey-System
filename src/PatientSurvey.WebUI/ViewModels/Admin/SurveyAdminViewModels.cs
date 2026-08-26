using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.DTOs.Doctor;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.WebUI.ViewModels.Shared;

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
    public bool IsGeneral { get; set; } = true;
    public int? DepartmentId { get; set; }
    public int? DoctorId { get; set; }
    public List<SurveyQuestionInputViewModel> Questions { get; set; } = new() { new SurveyQuestionInputViewModel() };
    public IReadOnlyCollection<DepartmentDto> Departments { get; set; } = Array.Empty<DepartmentDto>();
    public IReadOnlyCollection<DoctorOptionDto> Doctors { get; set; } = Array.Empty<DoctorOptionDto>();
}
