using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class QuestionsController : Controller
{
    private readonly QuestionService _questionService;
    private readonly SurveyService _surveyService;

    public QuestionsController(QuestionService questionService, SurveyService surveyService)
    {
        _questionService = questionService;
        _surveyService = surveyService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new QuestionIndexViewModel
        {
            Questions = await _questionService.GetAdminQuestionsAsync(cancellationToken)
        });
    }

    public async Task<IActionResult> Create(int? surveyId, CancellationToken cancellationToken)
    {
        return View(new CreateQuestionViewModel
        {
            SurveyId = surveyId,
            Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!viewModel.SurveyId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Anket secin.");
        }

        if (!viewModel.Type.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.Type), "Soru tipi secin.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
            return View(viewModel);
        }

        var result = await _questionService.CreateQuestionAsync(
            new CreateQuestionRequestDto(
                viewModel.SurveyId!.Value,
                viewModel.Text,
                viewModel.Type!.Value,
                viewModel.IsRequired,
                viewModel.IsActive,
                viewModel.DisplayOrder),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Soru olusturulamadi.");
            viewModel.Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var result = await _questionService.ToggleQuestionStatusAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
