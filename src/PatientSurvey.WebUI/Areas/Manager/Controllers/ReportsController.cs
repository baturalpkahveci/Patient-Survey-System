using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Report;
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

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? surveyScope,
        int? minResponses,
        CancellationToken cancellationToken)
    {
        var reports = await _reportService.GetSurveyReportsAsync(cancellationToken);
        var dashboard = await _reportService.GetManagerReportDashboardAsync(cancellationToken);
        var filtered = ApplyFilters(reports, search, status, minResponses).ToArray();
        var filteredDashboard = ApplyDashboardFilters(dashboard, search, status, surveyScope, minResponses);

        return View(new ReportIndexViewModel
        {
            Reports = filtered,
            Dashboard = filteredDashboard,
            Search = search,
            Status = status,
            SurveyScope = surveyScope,
            MinResponses = minResponses,
            TotalCount = dashboard.Surveys.Count + dashboard.Doctors.Count
        });
    }

    private static ManagerReportDashboardDto ApplyDashboardFilters(
        ManagerReportDashboardDto dashboard,
        string? search,
        string? status,
        string? surveyScope,
        int? minResponses)
    {
        var surveys = dashboard.Surveys.AsEnumerable();
        var doctors = dashboard.Doctors.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            surveys = surveys.Where(survey =>
                survey.SurveyTitle.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (survey.DoctorName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (survey.DepartmentName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            doctors = doctors.Where(doctor =>
                doctor.DoctorName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || doctor.DepartmentName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            surveys = surveys.Where(survey => survey.IsActive);
        }
        else if (string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase))
        {
            surveys = surveys.Where(survey => !survey.IsActive);
        }

        if (string.Equals(surveyScope, "general", StringComparison.OrdinalIgnoreCase))
        {
            surveys = surveys.Where(survey => string.Equals(survey.ScopeLabel, "Genel", StringComparison.OrdinalIgnoreCase));
        }
        else if (string.Equals(surveyScope, "targeted", StringComparison.OrdinalIgnoreCase))
        {
            surveys = surveys.Where(survey => string.Equals(survey.ScopeLabel, "Hedefli", StringComparison.OrdinalIgnoreCase));
        }

        if (minResponses.HasValue)
        {
            surveys = surveys.Where(survey => survey.ResponseCount >= minResponses.Value);
            doctors = doctors.Where(doctor => doctor.ResponseCount >= minResponses.Value);
        }

        var surveyArray = surveys.ToArray();
        var doctorArray = doctors.ToArray();

        return dashboard with
        {
            Surveys = surveyArray,
            Doctors = doctorArray,
            TopDoctors = doctorArray
                .Where(doctor => doctor.AverageScore.HasValue)
                .OrderByDescending(doctor => doctor.AverageScore)
                .ThenByDescending(doctor => doctor.ResponseCount)
                .Take(5)
                .Select(doctor => new PerformanceHighlightDto(doctor.DoctorName, doctor.DepartmentName, doctor.ResponseCount, doctor.AverageScore))
                .ToArray(),
            LowDoctors = doctorArray
                .Where(doctor => doctor.AverageScore.HasValue)
                .OrderBy(doctor => doctor.AverageScore)
                .ThenByDescending(doctor => doctor.ResponseCount)
                .Take(5)
                .Select(doctor => new PerformanceHighlightDto(doctor.DoctorName, doctor.DepartmentName, doctor.ResponseCount, doctor.AverageScore))
                .ToArray(),
            TopSurveys = surveyArray
                .Where(survey => survey.AverageScore.HasValue)
                .OrderByDescending(survey => survey.AverageScore)
                .ThenByDescending(survey => survey.ResponseCount)
                .Take(5)
                .Select(survey => new PerformanceHighlightDto(survey.SurveyTitle, survey.ScopeLabel, survey.ResponseCount, survey.AverageScore))
                .ToArray(),
            LowSurveys = surveyArray
                .Where(survey => survey.AverageScore.HasValue)
                .OrderBy(survey => survey.AverageScore)
                .ThenByDescending(survey => survey.ResponseCount)
                .Take(5)
                .Select(survey => new PerformanceHighlightDto(survey.SurveyTitle, survey.ScopeLabel, survey.ResponseCount, survey.AverageScore))
                .ToArray()
        };
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
