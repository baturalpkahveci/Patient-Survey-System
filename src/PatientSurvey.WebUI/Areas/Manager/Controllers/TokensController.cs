using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Manager.Controllers;

[Area("Manager")]
[Authorize(Roles = "Manager")]
public sealed class TokensController : Controller
{
    private readonly SurveyAccessTokenService _tokenService;
    private readonly SurveyService _surveyService;

    public TokensController(SurveyAccessTokenService tokenService, SurveyService surveyService)
    {
        _tokenService = tokenService;
        _surveyService = surveyService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new TokenIndexViewModel
        {
            Tokens = await _tokenService.GetTokensAsync(cancellationToken),
            SurveyUrlPrefix = $"{Request.Scheme}://{Request.Host}/Survey/"
        });
    }

    public async Task<IActionResult> Create(int? surveyId, CancellationToken cancellationToken)
    {
        return View(new CreateTokenViewModel
        {
            SurveyId = surveyId,
            Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTokenViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!viewModel.SurveyId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Anket seçin.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
            return View(viewModel);
        }

        var result = await _tokenService.CreateTokenAsync(
            new CreateSurveyAccessTokenRequestDto(viewModel.SurveyId!.Value, viewModel.ExpiresAtUtc),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Anket linki oluşturulamadı.");
            viewModel.Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }
}
