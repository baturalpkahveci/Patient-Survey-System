using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.DTOs.Department;

namespace PatientSurvey.Application.DTOs.Survey;

public sealed record SurveyFormDto(
    string Token,
    int? InvitationId,
    int SurveyId,
    string Title,
    string? Description,
    IReadOnlyCollection<SurveyQuestionDto> Questions,
    IReadOnlyCollection<DepartmentDto> Departments,
    string? KvkkNoticeVersion = null,
    string? KvkkNoticeText = null);
