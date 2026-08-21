using PatientSurvey.Application.DTOs.Question;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class QuestionIndexViewModel
{
    public IReadOnlyCollection<AdminQuestionListItemDto> Questions { get; set; } = Array.Empty<AdminQuestionListItemDto>();
}
