using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Manager.Controllers;

[Area("Manager")]
[Authorize(Roles = "Manager")]
public sealed class ResultsController : Controller
{
    private readonly ReportService _reportService;

    public ResultsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(
        int? surveyId,
        string? departmentName,
        DateTime? fromDate,
        DateTime? toDate,
        double? minAverage,
        CancellationToken cancellationToken)
    {
        var results = await _reportService.GetResultsAsync(cancellationToken);
        var filtered = ApplyFilters(results, surveyId, departmentName, fromDate, toDate, minAverage).ToArray();

        return View(new ResultIndexViewModel
        {
            Results = filtered,
            SurveyOptions = results
                .Where(result => result.SurveyId > 0)
                .GroupBy(result => new { result.SurveyId, result.SurveyTitle })
                .OrderBy(group => group.Key.SurveyTitle)
                .Select(group => new FilterOptionViewModel(group.Key.SurveyId.ToString(), group.Key.SurveyTitle))
                .ToArray(),
            DepartmentOptions = results
                .Where(result => !string.IsNullOrWhiteSpace(result.DepartmentName))
                .Select(result => result.DepartmentName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .Select(name => new FilterOptionViewModel(name, name))
                .ToArray(),
            SurveyId = surveyId,
            DepartmentName = departmentName,
            FromDate = fromDate,
            ToDate = toDate,
            MinAverage = minAverage,
            TotalCount = results.Count
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

    private static IEnumerable<SurveyResponseListItemDto> ApplyFilters(
        IReadOnlyCollection<SurveyResponseListItemDto> results,
        int? surveyId,
        string? departmentName,
        DateTime? fromDate,
        DateTime? toDate,
        double? minAverage)
    {
        var filtered = results.AsEnumerable();

        if (surveyId.HasValue)
        {
            filtered = filtered.Where(result => result.SurveyId == surveyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            filtered = filtered.Where(result => string.Equals(result.DepartmentName, departmentName, StringComparison.OrdinalIgnoreCase));
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(result => result.SubmittedAtUtc.LocalDateTime.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(result => result.SubmittedAtUtc.LocalDateTime.Date <= toDate.Value.Date);
        }

        if (minAverage.HasValue)
        {
            filtered = filtered.Where(result => result.AverageScore.HasValue && result.AverageScore.Value >= minAverage.Value);
        }

        return filtered;
    }
}
