using System.ComponentModel.DataAnnotations;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.WebUI.ViewModels.Shared;

public sealed class SurveyQuestionInputViewModel
{
    [Required(ErrorMessage = "Soru metni zorunludur.")]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.Score;
    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [Range(1, 1000, ErrorMessage = "Sıra 1 ile 1000 arasında olmalıdır.")]
    public int DisplayOrder { get; set; } = 1;
}
