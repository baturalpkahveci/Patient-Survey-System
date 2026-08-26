using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.Services;

public sealed class PatientVisitService
{
    private readonly IPatientVisitReadRepository _repository;
    private readonly PermissionService? _permissionService;

    public PatientVisitService(IPatientVisitReadRepository repository, PermissionService? permissionService = null)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyCollection<PatientVisitListItemDto>> GetPatientVisitsAsync(
        CancellationToken cancellationToken = default)
    {
        var canViewPatientPersonalData = _permissionService is not null
            && await _permissionService.CanCurrentUserViewPatientPersonalDataAsync("Hasta ziyaretleri", cancellationToken);
        var visits = await _repository.GetPatientVisitsAsync(canViewPatientPersonalData, cancellationToken);
        return ToListItems(visits, canViewPatientPersonalData);
    }

    public async Task<IReadOnlyCollection<PatientVisitListItemDto>> GetPatientVisitsByDoctorAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        var canViewPatientPersonalData = _permissionService is not null
            && await _permissionService.CanCurrentUserViewPatientPersonalDataAsync(
                "Doktor hasta ziyaretleri",
                cancellationToken);
        var visits = await _repository.GetPatientVisitsByDoctorAsync(
            doctorId,
            canViewPatientPersonalData,
            cancellationToken);
        return ToListItems(visits, canViewPatientPersonalData);
    }

    private static PatientVisitListItemDto[] ToListItems(
        IEnumerable<PatientVisit> visits,
        bool includePatientPersonalData)
    {
        return visits
            .OrderByDescending(visit => visit.ExaminedAtUtc)
            .ThenByDescending(visit => visit.Id)
            .Select(visit => ToListItem(visit, includePatientPersonalData))
            .ToArray();
    }

    private static PatientVisitListItemDto ToListItem(PatientVisit visit, bool includePatientPersonalData)
    {
        var patientName = includePatientPersonalData
            ? FormatPatientName(visit.Patient, visit.PatientId)
            : FormatPatientReference(visit.PatientId);
        var latestInvitation = visit.Invitations
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .ThenByDescending(invitation => invitation.Id)
            .FirstOrDefault();

        return new PatientVisitListItemDto(
            visit.Id,
            visit.PatientId,
            patientName,
            includePatientPersonalData ? MaskPatientName(visit.Patient, visit.PatientId) : FormatPatientReference(visit.PatientId),
            includePatientPersonalData ? Normalize(visit.Patient?.PhoneNumber) : null,
            includePatientPersonalData ? Normalize(visit.Patient?.Email) : null,
            visit.DoctorId,
            FormatDoctorName(visit.Doctor),
            visit.DepartmentId,
            visit.Department?.Name ?? "Bölüm yok",
            visit.CreatedByUser?.Username ?? "Sistem",
            visit.ExaminedAtUtc,
            visit.CreatedAtUtc,
            visit.Invitations.Count,
            latestInvitation?.Id,
            latestInvitation?.Survey?.Title,
            latestInvitation?.DeliveryStatus.ToString() ?? "None",
            FormatDeliveryStatus(latestInvitation?.DeliveryStatus),
            latestInvitation?.CreatedAtUtc);
    }

    private static string FormatPatientName(Patient? patient, int patientId)
    {
        if (patient is null)
        {
            return FormatPatientReference(patientId);
        }

        var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? FormatPatientReference(patientId) : fullName;
    }

    private static string MaskPatientName(Patient? patient, int patientId)
    {
        if (patient is null)
        {
            return FormatPatientReference(patientId);
        }

        var parts = new[] { patient.FirstName, patient.LastName }
            .Select(MaskPart)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? FormatPatientReference(patientId) : string.Join(" ", parts);
    }

    private static string FormatPatientReference(int patientId)
    {
        return patientId > 0 ? $"Hasta #{patientId}" : "Hasta bilgisi yok";
    }

    private static string MaskPart(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var visibleLength = trimmed.Length <= 2 ? 1 : 2;
        return $"{trimmed[..visibleLength]}***";
    }

    private static string FormatDoctorName(Doctor? doctor)
    {
        if (doctor is null)
        {
            return "Doktor yok";
        }

        var fullName = $"{doctor.FirstName} {doctor.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Doktor bilgisi yok" : $"Dr. {fullName}";
    }

    private static string FormatDeliveryStatus(SurveyDeliveryStatus? status)
    {
        return status switch
        {
            SurveyDeliveryStatus.LinkCreated => "Link oluşturuldu",
            SurveyDeliveryStatus.Sent => "Gönderildi",
            SurveyDeliveryStatus.Failed => "Başarısız",
            SurveyDeliveryStatus.NotConfigured => "Yapılandırılmadı",
            _ => "Davet yok"
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
