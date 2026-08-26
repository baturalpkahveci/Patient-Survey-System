using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;
using PatientSurvey.WebUI.ViewModels.PatientVisit;

namespace PatientSurvey.WebUI.Areas.Manager.Controllers;

[Area("Manager")]
[Authorize(Roles = "Manager")]
public sealed class PatientVisitsController : Controller
{
    private readonly PatientVisitService _patientVisitService;

    public PatientVisitsController(PatientVisitService patientVisitService)
    {
        _patientVisitService = patientVisitService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? departmentId,
        int? doctorId,
        string? deliveryStatus,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        CancellationToken cancellationToken)
    {
        var visits = await _patientVisitService.GetPatientVisitsAsync(cancellationToken);
        var filtered = SortVisits(ApplyFilters(visits, search, departmentId, doctorId, deliveryStatus, fromDate, toDate), sort).ToArray();

        return View(new PatientVisitIndexViewModel
        {
            Visits = filtered,
            DepartmentOptions = BuildDepartmentOptions(visits),
            DoctorOptions = BuildDoctorOptions(visits),
            DeliveryOptions = BuildDeliveryOptions(visits),
            Search = search,
            DepartmentId = departmentId,
            DoctorId = doctorId,
            DeliveryStatus = deliveryStatus,
            FromDate = fromDate,
            ToDate = toDate,
            Sort = NormalizeSort(sort),
            TotalCount = visits.Count,
            ShowPatientDetails = true,
            AreaName = "Manager",
            PageTitle = "Hasta Ziyaretleri",
            Description = "Hasta ziyaretlerini, bölüm-doktor bağlantılarını ve davet akışını izleyin."
        });
    }

    private static IEnumerable<PatientVisitListItemDto> ApplyFilters(
        IReadOnlyCollection<PatientVisitListItemDto> visits,
        string? search,
        int? departmentId,
        int? doctorId,
        string? deliveryStatus,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var filtered = visits.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(visit =>
                visit.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (visit.PatientPhone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (visit.PatientEmail?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || visit.DoctorName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || visit.DepartmentName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (visit.LatestSurveyTitle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || visit.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (departmentId.HasValue)
        {
            filtered = filtered.Where(visit => visit.DepartmentId == departmentId.Value);
        }

        if (doctorId.HasValue)
        {
            filtered = filtered.Where(visit => visit.DoctorId == doctorId.Value);
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
            "patient" => visits.OrderBy(visit => visit.PatientName).ThenByDescending(visit => visit.ExaminedAtUtc),
            "doctor" => visits.OrderBy(visit => visit.DoctorName).ThenByDescending(visit => visit.ExaminedAtUtc),
            "department" => visits.OrderBy(visit => visit.DepartmentName).ThenByDescending(visit => visit.ExaminedAtUtc),
            _ => visits.OrderByDescending(visit => visit.ExaminedAtUtc).ThenByDescending(visit => visit.Id)
        };
    }

    private static string NormalizeSort(string? sort)
    {
        return sort is "oldest" or "patient" or "doctor" or "department" ? sort : "newest";
    }

    private static IReadOnlyCollection<FilterOptionViewModel> BuildDepartmentOptions(IEnumerable<PatientVisitListItemDto> visits)
    {
        return visits
            .Where(visit => visit.DepartmentId.HasValue)
            .GroupBy(visit => new { Id = visit.DepartmentId!.Value, visit.DepartmentName })
            .OrderBy(group => group.Key.DepartmentName)
            .Select(group => new FilterOptionViewModel(group.Key.Id.ToString(), group.Key.DepartmentName))
            .ToArray();
    }

    private static IReadOnlyCollection<FilterOptionViewModel> BuildDoctorOptions(IEnumerable<PatientVisitListItemDto> visits)
    {
        return visits
            .Where(visit => visit.DoctorId.HasValue)
            .GroupBy(visit => new { Id = visit.DoctorId!.Value, visit.DoctorName })
            .OrderBy(group => group.Key.DoctorName)
            .Select(group => new FilterOptionViewModel(group.Key.Id.ToString(), group.Key.DoctorName))
            .ToArray();
    }

    private static IReadOnlyCollection<FilterOptionViewModel> BuildDeliveryOptions(IEnumerable<PatientVisitListItemDto> visits)
    {
        return visits
            .GroupBy(visit => new { visit.LatestDeliveryStatus, visit.LatestDeliveryStatusLabel })
            .OrderBy(group => group.Key.LatestDeliveryStatusLabel)
            .Select(group => new FilterOptionViewModel(group.Key.LatestDeliveryStatus, group.Key.LatestDeliveryStatusLabel))
            .ToArray();
    }
}
