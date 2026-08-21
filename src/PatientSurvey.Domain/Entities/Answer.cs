namespace PatientSurvey.Domain.Entities;

public sealed class Answer
{
    public int Id { get; set; }
    public int SurveyResponseId { get; set; }
    public int QuestionId { get; set; }
    public int? ScoreValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }

    public SurveyResponse? SurveyResponse { get; set; }
    public Question? Question { get; set; }
}
