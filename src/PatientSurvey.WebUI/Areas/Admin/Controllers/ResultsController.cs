using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ResultsController : Controller
{
    private readonly ReportService _reportService;

    public ResultsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new ResultIndexViewModel
        {
            Results = await _reportService.GetResultsAsync(cancellationToken)
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _reportService.GetResultDetailAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound();
        }

        return View(new ResultDetailViewModel { Result = result.Value });
    }
}
