using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.Services;

public sealed class PatientVisitService
{
    private readonly IPatientVisitReadRepository _repository;

    public PatientVisitService(IPatientVisitReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<PatientVisitListItemDto>> GetPatientVisitsAsync(
        CancellationToken cancellationToken = default)
    {
        var visits = await _repository.GetPatientVisitsAsync(cancellationToken);
        return ToListItems(visits);
    }

    public async Task<IReadOnlyCollection<PatientVisitListItemDto>> GetPatientVisitsByDoctorAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        var visits = await _repository.GetPatientVisitsByDoctorAsync(doctorId, cancellationToken);
        return ToListItems(visits);
    }

    private static PatientVisitListItemDto[] ToListItems(IEnumerable<PatientVisit> visits)
    {
        return visits
            .OrderByDescending(visit => visit.ExaminedAtUtc)
            .ThenByDescending(visit => visit.Id)
            .Select(ToListItem)
            .ToArray();
    }

    private static PatientVisitListItemDto ToListItem(PatientVisit visit)
    {
        var patientName = FormatPatientName(visit.Patient);
        var latestInvitation = visit.Invitations
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .ThenByDescending(invitation => invitation.Id)
            .FirstOrDefault();

        return new PatientVisitListItemDto(
            visit.Id,
            visit.PatientId,
            patientName,
            MaskPatientName(visit.Patient),
            Normalize(visit.Patient?.PhoneNumber),
            Normalize(visit.Patient?.Email),
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

    private static string FormatPatientName(Patient? patient)
    {
        if (patient is null)
        {
            return "Hasta bilgisi yok";
        }

        var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Hasta bilgisi yok" : fullName;
    }

    private static string MaskPatientName(Patient? patient)
    {
        if (patient is null)
        {
            return "Hasta ***";
        }

        var parts = new[] { patient.FirstName, patient.LastName }
            .Select(MaskPart)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? "Hasta ***" : string.Join(" ", parts);
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
