using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        var form = await _surveyService.GetSurveyFormAsync(token, cancellationToken);
        if (!form.IsSuccess || form.Value is null)
        {
            return View("Unavailable", form.Message);
        }

        return View("Index", ToViewModel(form.Value));
    }

    [HttpPost("Survey/{token}/Verify")]
    [EnableRateLimiting("SurveyIdentity")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(
        string token,
        SurveyIdentityViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Verify", viewModel);
        }

        var result = await _surveyService.VerifyPatientIdentityAsync(
            new VerifySurveyIdentityRequestDto(token, viewModel.TcIdentityNumber, viewModel.KvkkAccepted),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            var entry = await _surveyService.GetSurveyEntryAsync(token, cancellationToken);
            var hydrated = entry.Value is null ? viewModel : ToIdentityViewModel(entry.Value);
            hydrated.FormError = result.Message;
            return View("Verify", hydrated);
        }

        return RedirectToAction(nameof(Index), new { token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitSurveyViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await RehydrateAsync(viewModel, cancellationToken);
            return View("Index", viewModel);
        }

        int? verifiedInvitationId = null;
        string? consentVersion = null;
        if (viewModel.InvitationId.HasValue)
        {
            var identityResult = await _surveyService.VerifyPatientIdentityAsync(
                new VerifySurveyIdentityRequestDto(viewModel.Token, viewModel.TcIdentityNumber, viewModel.KvkkAccepted),
                cancellationToken);

            if (!identityResult.IsSuccess || identityResult.Value is null)
            {
                await RehydrateAsync(viewModel, cancellationToken);
                viewModel.FormError = identityResult.Message;
                return View("Index", viewModel);
            }

            verifiedInvitationId = identityResult.Value.InvitationId;
            consentVersion = identityResult.Value.NoticeVersion;
        }

        var request = new SubmitSurveyRequestDto(
            viewModel.Token,
            viewModel.DepartmentId,
            verifiedInvitationId,
            consentVersion,
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

    [HttpGet("Survey/ThankYou")]
    public IActionResult ThankYou()
    {
        return View();
    }

    private async Task RehydrateAsync(
        SubmitSurveyViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var formResult = await _surveyService.GetSurveyFormAsync(viewModel.Token, cancellationToken);

        if (formResult.Value is null)
        {
            return;
        }

        var existingAnswers = viewModel.Questions.ToDictionary(question => question.Id);
        var hydrated = ToViewModel(formResult.Value);
        hydrated.DepartmentId = viewModel.DepartmentId;
        hydrated.TcIdentityNumber = viewModel.TcIdentityNumber;
        hydrated.KvkkAccepted = viewModel.KvkkAccepted;
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

        viewModel.InvitationId = hydrated.InvitationId;
        viewModel.SurveyId = hydrated.SurveyId;
        viewModel.Title = hydrated.Title;
        viewModel.Description = hydrated.Description;
        viewModel.ConsentNoticeVersion = hydrated.ConsentNoticeVersion;
        viewModel.ConsentNoticeText = hydrated.ConsentNoticeText;
        viewModel.TcIdentityNumber = hydrated.TcIdentityNumber;
        viewModel.KvkkAccepted = hydrated.KvkkAccepted;
        viewModel.Departments = hydrated.Departments;
        viewModel.Questions = hydrated.Questions;
    }

    private static SubmitSurveyViewModel ToViewModel(SurveyFormDto dto)
    {
        return new SubmitSurveyViewModel
        {
            Token = dto.Token,
            InvitationId = dto.InvitationId,
            SurveyId = dto.SurveyId,
            Title = dto.Title,
            Description = dto.Description,
            ConsentNoticeVersion = dto.KvkkNoticeVersion,
            ConsentNoticeText = dto.KvkkNoticeText,
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

    private static SurveyIdentityViewModel ToIdentityViewModel(SurveyEntryDto dto)
    {
        return new SurveyIdentityViewModel
        {
            Token = dto.Token,
            InvitationId = dto.InvitationId,
            Title = dto.SurveyTitle,
            Description = dto.SurveyDescription,
            KvkkNoticeVersion = dto.KvkkNoticeVersion,
            KvkkNoticeText = dto.KvkkNoticeText
        };
    }
}
