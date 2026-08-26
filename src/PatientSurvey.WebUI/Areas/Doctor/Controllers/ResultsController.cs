using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Doctor;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class ResultsController : Controller
{
    private readonly ReportService _reportService;
    private readonly SurveyService _surveyService;
    private readonly DoctorService _doctorService;

    public ResultsController(
        ReportService reportService,
        SurveyService surveyService,
        DoctorService doctorService)
    {
        _reportService = reportService;
        _surveyService = surveyService;
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Index(
        int? surveyId,
        DateTime? fromDate,
        DateTime? toDate,
        double? minAverage,
        CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        var doctorId = profile.Value?.Id;
        var departmentId = profile.Value?.DepartmentId;
        var surveys = await GetCurrentDoctorSurveysAsync(doctorId, departmentId, cancellationToken);
        var results = await _reportService.GetResultsAsync(cancellationToken);
        var filtered = ApplyFilters(results, doctorId, surveyId, fromDate, toDate, minAverage).ToArray();

        return View(new DoctorResultIndexViewModel
        {
            Results = filtered,
            Surveys = surveys,
            SurveyId = surveyId,
            FromDate = fromDate,
            ToDate = toDate,
            MinAverage = minAverage,
            TotalCount = doctorId.HasValue
                ? results.Count(result => result.SurveyDoctorId == doctorId.Value)
                : 0
        });
    }

    private async Task<IReadOnlyCollection<PatientSurvey.Application.DTOs.Survey.AdminSurveyListItemDto>> GetCurrentDoctorSurveysAsync(
        int? doctorId,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        if (!doctorId.HasValue || !departmentId.HasValue)
        {
            return Array.Empty<PatientSurvey.Application.DTOs.Survey.AdminSurveyListItemDto>();
        }

        var surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
        return surveys
            .Where(survey => survey.DoctorId == doctorId.Value && survey.DepartmentId == departmentId.Value)
            .OrderBy(survey => survey.Title)
            .ToArray();
    }

    private static IEnumerable<SurveyResponseListItemDto> ApplyFilters(
        IReadOnlyCollection<SurveyResponseListItemDto> results,
        int? doctorId,
        int? surveyId,
        DateTime? fromDate,
        DateTime? toDate,
        double? minAverage)
    {
        var filtered = doctorId.HasValue
            ? results.Where(result => result.SurveyDoctorId == doctorId.Value)
            : Enumerable.Empty<SurveyResponseListItemDto>();

        if (surveyId.HasValue)
        {
            filtered = filtered.Where(result => result.SurveyId == surveyId.Value);
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

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
