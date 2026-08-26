namespace PatientSurvey.Application.DTOs.Survey;

public sealed record SurveyAccessTokenListItemDto(
    int Id,
    int SurveyId,
    int? InvitationId,
    string SurveyTitle,
    string Token,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? UsedAtUtc,
    bool HasResponse,
    string DeliveryStatus,
    int? SurveyDoctorId = null,
    int? SurveyDepartmentId = null,
    bool IsGeneralSurvey = true,
    string PatientName = "Anonim",
    string? PatientPhone = null,
    string? PatientEmail = null);
