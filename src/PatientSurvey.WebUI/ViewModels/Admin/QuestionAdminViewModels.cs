using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class CreateQuestionViewModel
{
    [Required(ErrorMessage = "Anket secin.")]
    public int? SurveyId { get; set; }

    [Required(ErrorMessage = "Soru metni zorunludur.")]
    public string Text { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soru tipi secin.")]
    public QuestionType? Type { get; set; } = QuestionType.Score;

    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [Range(1, 1000, ErrorMessage = "Sira 1 ile 1000 arasinda olmalidir.")]
    public int DisplayOrder { get; set; } = 1;

    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
}
