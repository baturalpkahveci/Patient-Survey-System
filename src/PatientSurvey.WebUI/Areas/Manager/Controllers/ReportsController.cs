using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Manager.Controllers;

[Area("Manager")]
[Authorize(Roles = "Manager")]
public sealed class ReportsController : Controller
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new ReportIndexViewModel
        {
            Reports = await _reportService.GetSurveyReportsAsync(cancellationToken)
        });
    }
}
