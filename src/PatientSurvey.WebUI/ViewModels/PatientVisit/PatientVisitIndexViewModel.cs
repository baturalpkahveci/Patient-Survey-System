using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Domain.Enums;
using PatientSurvey.WebUI.ViewModels.Admin;
using System.ComponentModel.DataAnnotations;

namespace PatientSurvey.WebUI.ViewModels.PatientVisit;

public sealed class PatientVisitIndexViewModel
{
    public IReadOnlyCollection<PatientVisitListItemDto> Visits { get; set; } = Array.Empty<PatientVisitListItemDto>();
    public IReadOnlyCollection<FilterOptionViewModel> DepartmentOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> DoctorOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> DeliveryOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public int? DoctorId { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Sort { get; set; } = "newest";
    public int TotalCount { get; set; }
    public bool ShowPatientDetails { get; set; }
    public bool ShowDoctorFilter { get; set; } = true;
    public bool ShowDepartmentFilter { get; set; } = true;
    public string AreaName { get; set; } = string.Empty;
    public string PageTitle { get; set; } = "Hasta Ziyaretleri";
    public string Description { get; set; } = string.Empty;
    public string EmptyMessage { get; set; } = "Filtreye uygun hasta ziyareti yok.";
    public string? CreateActionText { get; set; }
    public string CreateControllerName { get; set; } = "PatientVisits";
}

public sealed class CreatePatientVisitViewModel
{
    [Required(ErrorMessage = "Anket seçin.")]
    public int? SurveyId { get; set; }

    [Required(ErrorMessage = "Hasta adı zorunludur.")]
    public string PatientFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasta soyadı zorunludur.")]
    public string PatientLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "T.C. Kimlik Numarası zorunludur.")]
    public string TcIdentityNumber { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string? Email { get; set; }

    [DataType(DataType.DateTime)]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public SurveyDeliveryMethod DeliveryMethod { get; set; } = SurveyDeliveryMethod.LinkOnly;
    public CreatedSurveyInvitationDto? CreatedInvitation { get; set; }
    public string SurveyUrlPrefix { get; set; } = string.Empty;
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
}
