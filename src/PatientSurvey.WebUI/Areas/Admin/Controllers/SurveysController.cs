using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class SurveysController : Controller
{
    private readonly SurveyService _surveyService;

    public SurveysController(SurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new SurveyIndexViewModel
        {
            Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken)
        });
    }

    public IActionResult Create()
    {
        return View(new CreateSurveyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSurveyViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _surveyService.CreateSurveyAsync(
            new CreateSurveyRequestDto(viewModel.Title, viewModel.Description, viewModel.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Anket oluşturulamadı.");
            return View(viewModel);
        }

        return RedirectToAction("Create", "Questions", new { area = "Admin", surveyId = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var result = await _surveyService.ToggleSurveyStatusAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
