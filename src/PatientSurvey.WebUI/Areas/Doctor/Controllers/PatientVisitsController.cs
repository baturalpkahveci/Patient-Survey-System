using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;
using PatientSurvey.WebUI.ViewModels.PatientVisit;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class PatientVisitsController : Controller
{
    private readonly PatientVisitService _patientVisitService;
    private readonly DoctorService _doctorService;

    public PatientVisitsController(PatientVisitService patientVisitService, DoctorService doctorService)
    {
        _patientVisitService = patientVisitService;
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? deliveryStatus,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        IReadOnlyCollection<PatientVisitListItemDto> visits = profile.Value is null
            ? Array.Empty<PatientVisitListItemDto>()
            : await _patientVisitService.GetPatientVisitsByDoctorAsync(profile.Value.Id, cancellationToken);
        var filtered = SortVisits(ApplyFilters(visits, search, deliveryStatus, fromDate, toDate), sort).ToArray();

        return View(new PatientVisitIndexViewModel
        {
            Visits = filtered,
            DeliveryOptions = BuildDeliveryOptions(visits),
            Search = search,
            DeliveryStatus = deliveryStatus,
            FromDate = fromDate,
            ToDate = toDate,
            Sort = NormalizeSort(sort),
            TotalCount = visits.Count,
            ShowPatientDetails = false,
            ShowDoctorFilter = false,
            ShowDepartmentFilter = false,
            AreaName = "Doctor",
            PageTitle = "Hasta Ziyaretleri",
            Description = $"{profile.Value?.DepartmentName ?? "Bölüm"} bölümündeki ziyaretleri hasta kişisel bilgisi olmadan takip edin.",
            EmptyMessage = "Filtreye uygun ziyaret yok."
        });
    }

    private static IEnumerable<PatientVisitListItemDto> ApplyFilters(
        IReadOnlyCollection<PatientVisitListItemDto> visits,
        string? search,
        string? deliveryStatus,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var filtered = visits.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(visit =>
                visit.MaskedPatientName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || visit.DepartmentName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (visit.LatestSurveyTitle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || visit.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || (visit.LatestInvitationId?.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(deliveryStatus))
        {
            filtered = filtered.Where(visit => string.Equals(visit.LatestDeliveryStatus, deliveryStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(visit => visit.ExaminedAtUtc.LocalDateTime.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(visit => visit.ExaminedAtUtc.LocalDateTime.Date <= toDate.Value.Date);
        }

        return filtered;
    }

    private static IEnumerable<PatientVisitListItemDto> SortVisits(IEnumerable<PatientVisitListItemDto> visits, string? sort)
    {
        return NormalizeSort(sort) switch
        {
            "oldest" => visits.OrderBy(visit => visit.ExaminedAtUtc).ThenBy(visit => visit.Id),
            "patient" => visits.OrderBy(visit => visit.MaskedPatientName).ThenByDescending(visit => visit.ExaminedAtUtc),
            "department" => visits.OrderBy(visit => visit.DepartmentName).ThenByDescending(visit => visit.ExaminedAtUtc),
            _ => visits.OrderByDescending(visit => visit.ExaminedAtUtc).ThenByDescending(visit => visit.Id)
        };
    }

    private static string NormalizeSort(string? sort)
    {
        return sort is "oldest" or "patient" or "department" ? sort : "newest";
    }

    private static IReadOnlyCollection<FilterOptionViewModel> BuildDeliveryOptions(IEnumerable<PatientVisitListItemDto> visits)
    {
        return visits
            .GroupBy(visit => new { visit.LatestDeliveryStatus, visit.LatestDeliveryStatusLabel })
            .OrderBy(group => group.Key.LatestDeliveryStatusLabel)
            .Select(group => new FilterOptionViewModel(group.Key.LatestDeliveryStatus, group.Key.LatestDeliveryStatusLabel))
            .ToArray();
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
