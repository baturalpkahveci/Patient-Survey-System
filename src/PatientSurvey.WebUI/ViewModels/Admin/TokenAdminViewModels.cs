using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Survey;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class TokenIndexViewModel
{
    public IReadOnlyCollection<SurveyAccessTokenListItemDto> Tokens { get; set; } = Array.Empty<SurveyAccessTokenListItemDto>();
    public string SurveyUrlPrefix { get; set; } = string.Empty;
}

public sealed class CreateTokenViewModel
{
    [Required(ErrorMessage = "Anket secin.")]
    public int? SurveyId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
}
