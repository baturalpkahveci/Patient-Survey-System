using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;
using PatientSurvey.WebUI.ViewModels.Shared;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class SurveysController : Controller
{
    private readonly SurveyService _surveyService;
    private readonly QuestionService _questionService;

    public SurveysController(SurveyService surveyService, QuestionService questionService)
    {
        _surveyService = surveyService;
        _questionService = questionService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new SurveyIndexViewModel
        {
            Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken)
        });
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await HydrateCreateModelAsync(new CreateSurveyViewModel(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSurveyViewModel viewModel, CancellationToken cancellationToken)
    {
        NormalizeQuestions(viewModel.Questions);
        ValidateQuestions(viewModel.Questions);

        if (!ModelState.IsValid)
        {
            await HydrateCreateModelAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        var result = await _surveyService.CreateSurveyAsync(
            new CreateSurveyRequestDto(
                viewModel.Title,
                viewModel.Description,
                viewModel.IsActive,
                viewModel.IsGeneral,
                viewModel.DepartmentId,
                viewModel.DoctorId,
                GetCurrentUserId(),
                "Admin"),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Anket oluşturulamadı.");
            await HydrateCreateModelAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        foreach (var question in viewModel.Questions)
        {
            var questionResult = await _questionService.CreateQuestionAsync(
                new CreateQuestionRequestDto(
                    result.Value,
                    question.Text,
                    question.Type,
                    question.IsRequired,
                    question.IsActive,
                    question.DisplayOrder),
                cancellationToken);

            if (!questionResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, questionResult.Message ?? "Sorular oluşturulamadı.");
                await HydrateCreateModelAsync(viewModel, cancellationToken);
                return View(viewModel);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Doctors(int departmentId, CancellationToken cancellationToken)
    {
        var doctors = await _surveyService.GetActiveDoctorsByDepartmentAsync(departmentId, cancellationToken);
        return Json(doctors.Select(doctor => new { doctor.Id, doctor.DisplayName }));
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

    private async Task<CreateSurveyViewModel> HydrateCreateModelAsync(
        CreateSurveyViewModel viewModel,
        CancellationToken cancellationToken)
    {
        viewModel.Departments = await _surveyService.GetActiveDepartmentsAsync(cancellationToken);
        viewModel.Doctors = viewModel.DepartmentId.HasValue
            ? await _surveyService.GetActiveDoctorsByDepartmentAsync(viewModel.DepartmentId.Value, cancellationToken)
            : Array.Empty<PatientSurvey.Application.DTOs.Doctor.DoctorOptionDto>();
        NormalizeQuestions(viewModel.Questions);
        return viewModel;
    }

    private void ValidateQuestions(IReadOnlyCollection<SurveyQuestionInputViewModel> questions)
    {
        if (questions.Count == 0)
        {
            ModelState.AddModelError(nameof(CreateSurveyViewModel.Questions), "En az bir soru ekleyin.");
            return;
        }

        foreach (var question in questions.Select((value, index) => new { value, index }))
        {
            if (string.IsNullOrWhiteSpace(question.value.Text))
            {
                ModelState.AddModelError($"Questions[{question.index}].Text", "Soru metni zorunludur.");
            }
        }
    }

    private static void NormalizeQuestions(List<SurveyQuestionInputViewModel> questions)
    {
        if (questions.Count == 0)
        {
            questions.Add(new SurveyQuestionInputViewModel());
        }

        for (var index = 0; index < questions.Count; index++)
        {
            if (questions[index].DisplayOrder <= 0)
            {
                questions[index].DisplayOrder = index + 1;
            }
        }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
