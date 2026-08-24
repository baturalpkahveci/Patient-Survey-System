using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Report;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController : Controller
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int? minResponses,
        CancellationToken cancellationToken)
    {
        var reports = await _reportService.GetSurveyReportsAsync(cancellationToken);
        var filtered = ApplyFilters(reports, search, status, minResponses).ToArray();

        return View(new ReportIndexViewModel
        {
            Reports = filtered,
            Search = search,
            Status = status,
            MinResponses = minResponses,
            TotalCount = reports.Count
        });
    }

    private static IEnumerable<SurveyReportDto> ApplyFilters(
        IReadOnlyCollection<SurveyReportDto> reports,
        string? search,
        string? status,
        int? minResponses)
    {
        var filtered = reports.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(report => report.SurveyTitle.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(report => report.IsActive);
        }
        else if (string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(report => !report.IsActive);
        }

        if (minResponses.HasValue)
        {
            filtered = filtered.Where(report => report.ResponseCount >= minResponses.Value);
        }

        return filtered;
    }
}
