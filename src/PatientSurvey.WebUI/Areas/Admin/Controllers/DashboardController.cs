using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController : Controller
{
    private readonly ReportService _reportService;
    private readonly PatientVisitService _patientVisitService;

    public DashboardController(ReportService reportService, PatientVisitService patientVisitService)
    {
        _reportService = reportService;
        _patientVisitService = patientVisitService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var visits = await _patientVisitService.GetPatientVisitsAsync(cancellationToken);

        return View(new DashboardViewModel
        {
            Overview = await _reportService.GetDashboardOverviewAsync(cancellationToken),
            AreaName = "Admin",
            PatientVisitCount = visits.Count
        });
    }
}
