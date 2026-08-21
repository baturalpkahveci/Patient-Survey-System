using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Survey;

namespace PatientSurvey.WebUI.Controllers;

public sealed class SurveyController : Controller
{
    private readonly SurveyService _surveyService;
    private readonly SurveySubmissionService _submissionService;

    public SurveyController(SurveyService surveyService, SurveySubmissionService submissionService)
    {
        _surveyService = surveyService;
        _submissionService = submissionService;
    }

    [HttpGet("Survey/{token}")]
    public async Task<IActionResult> Index(string token, CancellationToken cancellationToken)
    {
        var result = await _surveyService.GetSurveyFormAsync(token, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return View("Unavailable", result.Message);
        }

        return View(ToViewModel(result.Value));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitSurveyViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!viewModel.DepartmentId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.DepartmentId), "Lütfen bölüm seçin.");
        }

        if (!ModelState.IsValid)
        {
            await RehydrateAsync(viewModel, cancellationToken);
            return View("Index", viewModel);
        }

        var request = new SubmitSurveyRequestDto(
            viewModel.Token,
            viewModel.DepartmentId!.Value,
            viewModel.Questions.Select(question => new SubmitAnswerDto(
                question.Id,
                question.ScoreValue,
                question.TextValue,
                question.BooleanValue)).ToArray());

        var result = await _submissionService.SubmitAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            await RehydrateAsync(viewModel, cancellationToken);
            viewModel.FormError = result.Message;
            return View("Index", viewModel);
        }

        return RedirectToAction(nameof(ThankYou));
    }

    public IActionResult ThankYou()
    {
        return View();
    }

    private async Task RehydrateAsync(SubmitSurveyViewModel viewModel, CancellationToken cancellationToken)
    {
        var formResult = await _surveyService.GetSurveyFormAsync(viewModel.Token, cancellationToken);
        if (formResult.Value is null)
        {
            return;
        }

        var existingAnswers = viewModel.Questions.ToDictionary(question => question.Id);
        var hydrated = ToViewModel(formResult.Value);
        hydrated.DepartmentId = viewModel.DepartmentId;
        hydrated.FormError = viewModel.FormError;

        foreach (var question in hydrated.Questions)
        {
            if (!existingAnswers.TryGetValue(question.Id, out var existing))
            {
                continue;
            }

            question.ScoreValue = existing.ScoreValue;
            question.TextValue = existing.TextValue;
            question.BooleanValue = existing.BooleanValue;
        }

        viewModel.SurveyId = hydrated.SurveyId;
        viewModel.Title = hydrated.Title;
        viewModel.Description = hydrated.Description;
        viewModel.Departments = hydrated.Departments;
        viewModel.Questions = hydrated.Questions;
    }

    private static SubmitSurveyViewModel ToViewModel(SurveyFormDto dto)
    {
        return new SubmitSurveyViewModel
        {
            Token = dto.Token,
            SurveyId = dto.SurveyId,
            Title = dto.Title,
            Description = dto.Description,
            Departments = dto.Departments.Select(department => new DepartmentOptionViewModel
            {
                Id = department.Id,
                Name = department.Name
            }).ToList(),
            Questions = dto.Questions.Select(question => new SurveyQuestionViewModel
            {
                Id = question.Id,
                Text = question.Text,
                Type = question.Type,
                IsRequired = question.IsRequired,
                DisplayOrder = question.DisplayOrder
            }).ToList()
        };
    }
}
