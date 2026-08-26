using PatientSurvey.Domain.Enums;

namespace PatientSurvey.WebUI.ViewModels.Survey;

public sealed class SubmitSurveyViewModel
{
    public string Token { get; set; } = string.Empty;
    public int? InvitationId { get; set; }
    public int SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ConsentNoticeVersion { get; set; }
    public string? ConsentNoticeText { get; set; }
    public string TcIdentityNumber { get; set; } = string.Empty;
    public bool KvkkAccepted { get; set; }

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

public sealed class SurveyIdentityViewModel
{
    public string Token { get; set; } = string.Empty;
    public int InvitationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string TcIdentityNumber { get; set; } = string.Empty;

    public bool KvkkAccepted { get; set; }

    public string KvkkNoticeVersion { get; set; } = string.Empty;
    public string KvkkNoticeText { get; set; } = string.Empty;
    public string? FormError { get; set; }
}
