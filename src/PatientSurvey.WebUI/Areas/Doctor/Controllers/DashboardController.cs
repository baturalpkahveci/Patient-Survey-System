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

    public DashboardController(DoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);

        return View(new DoctorDashboardViewModel
        {
            DisplayName = profile.Value is null
                ? User.Identity?.Name ?? "Doktor"
                : $"Dr. {profile.Value.FirstName} {profile.Value.LastName}",
            DepartmentName = profile.Value?.DepartmentName ?? "Bölüm bilgisi bulunamadı"
        });
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
