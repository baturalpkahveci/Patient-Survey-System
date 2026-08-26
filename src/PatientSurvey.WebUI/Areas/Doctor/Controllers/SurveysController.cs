using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Doctor;
using PatientSurvey.WebUI.ViewModels.Shared;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class SurveysController : Controller
{
    private readonly SurveyService _surveyService;
    private readonly DoctorService _doctorService;
    private readonly QuestionService _questionService;

    public SurveysController(
        SurveyService surveyService,
        DoctorService doctorService,
        QuestionService questionService)
    {
        _surveyService = surveyService;
        _doctorService = doctorService;
        _questionService = questionService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        var surveys = await GetCurrentDoctorSurveysAsync(profile.Value?.Id, profile.Value?.DepartmentId, cancellationToken);

        return View(new DoctorSurveyIndexViewModel
        {
            Surveys = surveys,
            DisplayName = profile.Value is null ? User.Identity?.Name ?? "Doktor" : $"Dr. {profile.Value.FirstName} {profile.Value.LastName}",
            DepartmentName = profile.Value?.DepartmentName ?? "Bölüm bilgisi bulunamadı"
        });
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        return View(new DoctorCreateSurveyViewModel
        {
            DepartmentName = profile.Value?.DepartmentName ?? "Bölüm bilgisi bulunamadı"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoctorCreateSurveyViewModel viewModel, CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        viewModel.DepartmentName = profile.Value?.DepartmentName ?? "Bölüm bilgisi bulunamadı";
        NormalizeQuestions(viewModel.Questions);
        ValidateQuestions(viewModel.Questions);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _surveyService.CreateSurveyAsync(
            new CreateSurveyRequestDto(
                viewModel.Title,
                viewModel.Description,
                viewModel.IsActive,
                IsGeneral: false,
                DepartmentId: null,
                DoctorId: null,
                CreatedByUserId: GetCurrentUserId(),
                CreatedByRole: "Doctor"),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Anket oluşturulamadı.");
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
                return View(viewModel);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyCollection<AdminSurveyListItemDto>> GetCurrentDoctorSurveysAsync(
        int? doctorId,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        if (!doctorId.HasValue || !departmentId.HasValue)
        {
            return Array.Empty<AdminSurveyListItemDto>();
        }

        var surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
        return surveys
            .Where(survey => survey.DoctorId == doctorId.Value && survey.DepartmentId == departmentId.Value)
            .OrderBy(survey => survey.Title)
            .ToArray();
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private void ValidateQuestions(IReadOnlyCollection<SurveyQuestionInputViewModel> questions)
    {
        if (questions.Count == 0)
        {
            ModelState.AddModelError(nameof(DoctorCreateSurveyViewModel.Questions), "En az bir soru ekleyin.");
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
}
