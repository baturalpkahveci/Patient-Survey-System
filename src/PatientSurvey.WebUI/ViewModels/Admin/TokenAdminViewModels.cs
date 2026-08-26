using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class TokenIndexViewModel
{
    public IReadOnlyCollection<SurveyAccessTokenListItemDto> Tokens { get; set; } = Array.Empty<SurveyAccessTokenListItemDto>();
    public IReadOnlyCollection<FilterOptionViewModel> SurveyOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public IReadOnlyCollection<FilterOptionViewModel> DeliveryOptions { get; set; } = Array.Empty<FilterOptionViewModel>();
    public string SurveyUrlPrefix { get; set; } = string.Empty;
    public string? Search { get; set; }
    public int? SurveyId { get; set; }
    public string? Status { get; set; }
    public string? DeliveryStatus { get; set; }
    public string? SurveyScope { get; set; }
    public int TotalCount { get; set; }
}

public sealed class CreateTokenViewModel
{
    [Required(ErrorMessage = "Anket seçin.")]
    public int? SurveyId { get; set; }

    [Required(ErrorMessage = "Hasta ziyareti seçin.")]
    public int? PatientVisitId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public SurveyDeliveryMethod DeliveryMethod { get; set; } = SurveyDeliveryMethod.LinkOnly;
    public CreatedSurveyInvitationDto? CreatedInvitation { get; set; }
    public string SurveyUrlPrefix { get; set; } = string.Empty;
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
    public IReadOnlyCollection<PatientVisitListItemDto> PatientVisits { get; set; } = Array.Empty<PatientVisitListItemDto>();
}
