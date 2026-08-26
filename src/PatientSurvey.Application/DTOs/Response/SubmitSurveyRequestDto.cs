namespace PatientSurvey.Application.DTOs.Response;

public sealed record SubmitSurveyRequestDto(
    string Token,
    int? DepartmentId,
    int? VerifiedSurveyInvitationId,
    string? ConsentNoticeVersion,
    IReadOnlyCollection<SubmitAnswerDto> Answers)
{
    public SubmitSurveyRequestDto(
        string token,
        int departmentId,
        IReadOnlyCollection<SubmitAnswerDto> answers)
        : this(token, departmentId, null, null, answers)
    {
    }
}
