namespace PatientSurvey.Application.DTOs.Response;

public sealed record SubmitSurveyRequestDto(
    string Token,
    int DepartmentId,
    IReadOnlyCollection<SubmitAnswerDto> Answers);
