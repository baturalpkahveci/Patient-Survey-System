using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Domain.Enums;
using PatientSurvey.WebUI.ViewModels.Admin;
using PatientSurvey.WebUI.ViewModels.Shared;

namespace PatientSurvey.WebUI.ViewModels.Doctor;

public sealed class DoctorDashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public sealed class DoctorSurveyIndexViewModel
{
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public sealed class DoctorCreateSurveyViewModel
{
    [Required(ErrorMessage = "Anket başlığı zorunludur.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string DepartmentName { get; set; } = string.Empty;
    public List<SurveyQuestionInputViewModel> Questions { get; set; } = new() { new SurveyQuestionInputViewModel() };
}

public sealed class DoctorPatientRecordViewModel
{
    [Required(ErrorMessage = "Anket seçin.")]
    public int? SurveyId { get; set; }

    [Required(ErrorMessage = "Hasta adı zorunludur.")]
    public string PatientFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasta soyadı zorunludur.")]
    public string PatientLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "T.C. Kimlik Numarası zorunludur.")]
    public string TcIdentityNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon zorunludur.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public SurveyDeliveryMethod DeliveryMethod { get; set; } = SurveyDeliveryMethod.LinkOnly;
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
    public CreatedSurveyInvitationDto? CreatedInvitation { get; set; }
    public string SurveyUrlPrefix { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public sealed class DoctorResultIndexViewModel
{
    public IReadOnlyCollection<SurveyResponseListItemDto> Results { get; set; } = Array.Empty<SurveyResponseListItemDto>();
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
    public int? SurveyId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public double? MinAverage { get; set; }
    public int TotalCount { get; set; }
}

public sealed class DoctorTokenIndexViewModel
{
    public IReadOnlyCollection<SurveyAccessTokenListItemDto> Tokens { get; set; } = Array.Empty<SurveyAccessTokenListItemDto>();
    public IReadOnlyCollection<FilterOptionViewModel> SurveyOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> DeliveryOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public string SurveyUrlPrefix { get; set; } = string.Empty;
    public string? Search { get; set; }
    public int? SurveyId { get; set; }
    public string? DeliveryStatus { get; set; }
    public int TotalCount { get; set; }
}
