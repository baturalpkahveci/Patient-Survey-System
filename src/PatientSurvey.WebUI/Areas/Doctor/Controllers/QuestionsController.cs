using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class QuestionsController : Controller
{
    private readonly QuestionService _questionService;
    private readonly SurveyService _surveyService;
    private readonly DoctorService _doctorService;

    public QuestionsController(
        QuestionService questionService,
        SurveyService surveyService,
        DoctorService doctorService)
    {
        _questionService = questionService;
        _surveyService = surveyService;
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Create(int? surveyId, CancellationToken cancellationToken)
    {
        return View(await HydrateAsync(new CreateQuestionViewModel { SurveyId = surveyId }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionViewModel viewModel, CancellationToken cancellationToken)
    {
        var hydrated = await HydrateAsync(viewModel, cancellationToken);
        if (!viewModel.SurveyId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Anket seçin.");
        }

        if (!viewModel.Type.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.Type), "Soru tipi seçin.");
        }

        if (viewModel.SurveyId.HasValue && hydrated.Surveys.All(survey => survey.Id != viewModel.SurveyId.Value))
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Sadece kendi anketlerinize soru ekleyebilirsiniz.");
        }

        if (!ModelState.IsValid)
        {
            return View(hydrated);
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
            return View(hydrated);
        }

        return RedirectToAction("Index", "Surveys", new { area = "Doctor" });
    }

    private async Task<CreateQuestionViewModel> HydrateAsync(
        CreateQuestionViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        var doctorId = profile.Value?.Id;
        var departmentId = profile.Value?.DepartmentId;
        var surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);

        viewModel.Surveys = doctorId.HasValue && departmentId.HasValue
            ? surveys
                .Where(survey => survey.DoctorId == doctorId && survey.DepartmentId == departmentId)
                .OrderBy(survey => survey.Title)
                .ToArray()
            : Array.Empty<PatientSurvey.Application.DTOs.Survey.AdminSurveyListItemDto>();

        return viewModel;
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
