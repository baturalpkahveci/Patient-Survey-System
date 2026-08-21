using System.ComponentModel.DataAnnotations;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.WebUI.ViewModels.Survey;

public sealed class SubmitSurveyViewModel
{
    public string Token { get; set; } = string.Empty;
    public int SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Required(ErrorMessage = "Lütfen bölüm seçin.")]
    public int? DepartmentId { get; set; }

    public List<DepartmentOptionViewModel> Departments { get; set; } = new();
    public List<SurveyQuestionViewModel> Questions { get; set; } = new();
    public string? FormError { get; set; }
}

public sealed class DepartmentOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class SurveyQuestionViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public int? ScoreValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
}
