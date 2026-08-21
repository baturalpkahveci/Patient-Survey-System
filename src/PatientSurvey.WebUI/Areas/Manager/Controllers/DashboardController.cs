using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Manager.Controllers;

[Area("Manager")]
[Authorize(Roles = "Manager")]
public sealed class DashboardController : Controller
{
    private readonly ReportService _reportService;

    public DashboardController(ReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new DashboardViewModel
        {
            Overview = await _reportService.GetDashboardOverviewAsync(cancellationToken),
            AreaName = "Manager"
        });
    }
}
