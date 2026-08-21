using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Domain.Entities;

public sealed class Question
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Survey? Survey { get; set; }
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
