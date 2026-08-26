using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Doctor;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class DashboardController : Controller
{
    private readonly DoctorService _doctorService;
    private readonly PatientVisitService _patientVisitService;

    public DashboardController(DoctorService doctorService, PatientVisitService patientVisitService)
    {
        _doctorService = doctorService;
        _patientVisitService = patientVisitService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        var patientVisitCount = profile.Value is null
            ? 0
            : (await _patientVisitService.GetPatientVisitsByDoctorAsync(profile.Value.Id, cancellationToken)).Count;

        return View(new DoctorDashboardViewModel
        {
            DisplayName = profile.Value is null
                ? User.Identity?.Name ?? "Doktor"
                : $"Dr. {profile.Value.FirstName} {profile.Value.LastName}",
            DepartmentName = profile.Value?.DepartmentName ?? "Bölüm bilgisi bulunamadı",
            PatientVisitCount = patientVisitCount
        });
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
