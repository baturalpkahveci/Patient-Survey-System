using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.PatientVisit;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;
using PatientSurvey.WebUI.ViewModels.PatientVisit;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class PatientVisitsController : Controller
{
    private readonly PatientVisitService _patientVisitService;
    private readonly SurveyService _surveyService;
    private readonly SurveyInvitationService _invitationService;

    public PatientVisitsController(
        PatientVisitService patientVisitService,
        SurveyService surveyService,
        SurveyInvitationService invitationService)
    {
        _patientVisitService = patientVisitService;
        _surveyService = surveyService;
        _invitationService = invitationService;
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
            AreaName = "Admin",
            CreateActionText = "Yeni Hasta Ziyareti",
            PageTitle = "Hasta Ziyaretleri",
            Description = "Hasta ziyaretlerini, bağlı doktorları ve davet geçmişini tam görünümle takip edin."
        });
    }

    public async Task<IActionResult> Create(int? surveyId, CancellationToken cancellationToken)
    {
        return View(await HydrateCreateModelAsync(new CreatePatientVisitViewModel { SurveyId = surveyId }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePatientVisitViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!viewModel.SurveyId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Anket seçin.");
        }

        if (!ModelState.IsValid)
        {
            return View(await HydrateCreateModelAsync(viewModel, cancellationToken));
        }

        var result = await _invitationService.CreateInvitationAsync(
            new CreateSurveyInvitationRequestDto(
                viewModel.SurveyId!.Value,
                viewModel.PatientFirstName,
                viewModel.PatientLastName,
                viewModel.TcIdentityNumber,
                viewModel.PhoneNumber,
                viewModel.Email,
                viewModel.DeliveryMethod,
                viewModel.ExpiresAtUtc,
                GetCurrentUserId(),
                GetSurveyUrlPrefix()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Hasta ziyareti oluşturulamadı.");
            return View(await HydrateCreateModelAsync(viewModel, cancellationToken));
        }

        var resultModel = await HydrateCreateModelAsync(new CreatePatientVisitViewModel(), cancellationToken);
        resultModel.CreatedInvitation = result.Value;
        ModelState.Clear();
        return View(resultModel);
    }

    private async Task<CreatePatientVisitViewModel> HydrateCreateModelAsync(
        CreatePatientVisitViewModel viewModel,
        CancellationToken cancellationToken)
    {
        viewModel.Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
        viewModel.SurveyUrlPrefix = GetSurveyUrlPrefix();
        return viewModel;
    }

    private string GetSurveyUrlPrefix()
    {
        return $"{Request.Scheme}://{Request.Host}/Survey/";
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
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
