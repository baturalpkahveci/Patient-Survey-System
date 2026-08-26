using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Doctor;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class PatientsController : Controller
{
    private readonly SurveyService _surveyService;
    private readonly SurveyInvitationService _invitationService;
    private readonly DoctorService _doctorService;

    public PatientsController(
        SurveyService surveyService,
        SurveyInvitationService invitationService,
        DoctorService doctorService)
    {
        _surveyService = surveyService;
        _invitationService = invitationService;
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Create(int? surveyId, CancellationToken cancellationToken)
    {
        return View(await HydrateAsync(new DoctorPatientRecordViewModel { SurveyId = surveyId }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoctorPatientRecordViewModel viewModel, CancellationToken cancellationToken)
    {
        var hydrated = await HydrateAsync(viewModel, cancellationToken);
        if (!viewModel.SurveyId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Anket seçin.");
        }

        if (viewModel.SurveyId.HasValue && hydrated.Surveys.All(survey => survey.Id != viewModel.SurveyId.Value))
        {
            ModelState.AddModelError(nameof(viewModel.SurveyId), "Sadece kendi bölümünüz için oluşturduğunuz anketleri seçebilirsiniz.");
        }

        if (!ModelState.IsValid)
        {
            return View(hydrated);
        }

        var invitationResult = await _invitationService.CreateInvitationAsync(
            new CreateSurveyInvitationRequestDto(
                viewModel.SurveyId!.Value,
                viewModel.PatientFirstName,
                viewModel.PatientLastName,
                viewModel.TcIdentityNumber,
                viewModel.PhoneNumber,
                viewModel.Email,
                viewModel.DeliveryMethod,
                viewModel.ExpiresAtUtc,
                GetCurrentUserId(),
                GetSurveyUrlPrefix()),
            cancellationToken);

        if (!invitationResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, invitationResult.Message ?? "Davet linki oluşturulamadı.");
            return View(hydrated);
        }

        var resultModel = await HydrateAsync(new DoctorPatientRecordViewModel(), cancellationToken);
        resultModel.CreatedInvitation = invitationResult.Value;
        ModelState.Clear();
        return View(resultModel);
    }

    private async Task<DoctorPatientRecordViewModel> HydrateAsync(
        DoctorPatientRecordViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        viewModel.DepartmentName = profile.Value?.DepartmentName ?? "Bölüm bilgisi bulunamadı";
        viewModel.SurveyUrlPrefix = GetSurveyUrlPrefix();

        var doctorId = profile.Value?.Id;
        var departmentId = profile.Value?.DepartmentId;
        var surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
        viewModel.Surveys = surveys
            .Where(survey => survey.DoctorId == doctorId && survey.DepartmentId == departmentId && survey.IsActive)
            .OrderBy(survey => survey.Title)
            .ToArray();

        return viewModel;
    }

    private string GetSurveyUrlPrefix()
    {
        return $"{Request.Scheme}://{Request.Host}/Survey/";
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
