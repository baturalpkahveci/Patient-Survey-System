namespace PatientSurvey.Application.DTOs.PatientVisit;

public sealed record PatientVisitListItemDto(
    int Id,
    int PatientId,
    string PatientName,
    string MaskedPatientName,
    string? PatientPhone,
    string? PatientEmail,
    int? DoctorId,
    string DoctorName,
    int? DepartmentId,
    string DepartmentName,
    string CreatedByUsername,
    DateTimeOffset ExaminedAtUtc,
    DateTimeOffset CreatedAtUtc,
    int InvitationCount,
    int? LatestInvitationId,
    string? LatestSurveyTitle,
    string LatestDeliveryStatus,
    string LatestDeliveryStatusLabel,
    DateTimeOffset? LatestInvitationCreatedAtUtc);
