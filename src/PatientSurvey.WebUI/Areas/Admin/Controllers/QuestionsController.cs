using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Enums;
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

    public async Task<IActionResult> Index(
        int? surveyId,
        QuestionType? type,
        string? status,
        string? search,
        CancellationToken cancellationToken)
    {
        var questions = await _questionService.GetAdminQuestionsAsync(cancellationToken);
        var filtered = ApplyFilters(questions, surveyId, type, status, search);

        return View(new QuestionIndexViewModel
        {
            Questions = filtered.ToArray(),
            Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken),
            SurveyId = surveyId,
            Type = type,
            Status = status,
            Search = search,
            TotalCount = questions.Count
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
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Anket seçin.");
        }

        if (!viewModel.Type.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.Type), "Soru tipi seçin.");
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
            ModelState.AddModelError(string.Empty, result.Message ?? "Soru oluşturulamadı.");
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

    private static IEnumerable<AdminQuestionListItemDto> ApplyFilters(
        IReadOnlyCollection<AdminQuestionListItemDto> questions,
        int? surveyId,
        QuestionType? type,
        string? status,
        string? search)
    {
        var filtered = questions.AsEnumerable();

        if (surveyId.HasValue)
        {
            filtered = filtered.Where(question => question.SurveyId == surveyId.Value);
        }

        if (type.HasValue)
        {
            filtered = filtered.Where(question => question.Type == type.Value);
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(question => question.IsActive);
        }
        else if (string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(question => !question.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(question =>
                question.Text.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                question.SurveyTitle.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }
}
