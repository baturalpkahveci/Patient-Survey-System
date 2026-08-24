using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class QuestionIndexViewModel
{
    public IReadOnlyCollection<AdminQuestionListItemDto> Questions { get; set; } = Array.Empty<AdminQuestionListItemDto>();
    public IReadOnlyCollection<AdminSurveyListItemDto> Surveys { get; set; } = Array.Empty<AdminSurveyListItemDto>();
    public int? SurveyId { get; set; }
    public QuestionType? Type { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int TotalCount { get; set; }
}
