using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class TokensController : Controller
{
    private readonly SurveyAccessTokenService _tokenService;
    private readonly SurveyService _surveyService;
    private readonly SurveyInvitationService _invitationService;

    public TokensController(
        SurveyAccessTokenService tokenService,
        SurveyService surveyService,
        SurveyInvitationService invitationService)
    {
        _tokenService = tokenService;
        _surveyService = surveyService;
        _invitationService = invitationService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? surveyId,
        string? status,
        string? deliveryStatus,
        string? surveyScope,
        CancellationToken cancellationToken)
    {
        var tokens = await _tokenService.GetTokensAsync(cancellationToken);
        var filtered = ApplyFilters(tokens, search, surveyId, status, deliveryStatus, surveyScope).ToArray();

        return View(new TokenIndexViewModel
        {
            Tokens = filtered,
            SurveyOptions = tokens
                .GroupBy(token => new { token.SurveyId, token.SurveyTitle })
                .OrderBy(group => group.Key.SurveyTitle)
                .Select(group => new FilterOptionViewModel(group.Key.SurveyId.ToString(), group.Key.SurveyTitle))
                .ToArray(),
            DeliveryOptions = tokens
                .Select(token => token.DeliveryStatus)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .Select(value => new FilterOptionViewModel(value, FormatDeliveryStatus(value)))
                .ToArray(),
            SurveyUrlPrefix = GetSurveyUrlPrefix(),
            Search = search,
            SurveyId = surveyId,
            Status = status,
            DeliveryStatus = deliveryStatus,
            SurveyScope = surveyScope,
            TotalCount = tokens.Count
        });
    }

    public async Task<IActionResult> Create(int? surveyId, CancellationToken cancellationToken)
    {
        return View(new CreateTokenViewModel
        {
            SurveyId = surveyId,
            Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken),
            SurveyUrlPrefix = GetSurveyUrlPrefix()
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
            await HydrateCreateModelAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        var result = await _invitationService.CreateInvitationAsync(
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

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Anket linki oluşturulamadı.");
            await HydrateCreateModelAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        viewModel.CreatedInvitation = result.Value;
        viewModel.PatientFirstName = string.Empty;
        viewModel.PatientLastName = string.Empty;
        viewModel.TcIdentityNumber = string.Empty;
        viewModel.PhoneNumber = string.Empty;
        viewModel.Email = string.Empty;
        ModelState.Clear();
        await HydrateCreateModelAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    private async Task HydrateCreateModelAsync(CreateTokenViewModel viewModel, CancellationToken cancellationToken)
    {
        viewModel.Surveys = await _surveyService.GetAdminSurveysAsync(cancellationToken);
        viewModel.SurveyUrlPrefix = GetSurveyUrlPrefix();
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

    private static IEnumerable<SurveyAccessTokenListItemDto> ApplyFilters(
        IReadOnlyCollection<SurveyAccessTokenListItemDto> tokens,
        string? search,
        int? surveyId,
        string? status,
        string? deliveryStatus,
        string? surveyScope)
    {
        var filtered = tokens.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(token =>
                token.SurveyTitle.Contains(term, StringComparison.OrdinalIgnoreCase)
                || token.PatientName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || token.Token.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (surveyId.HasValue)
        {
            filtered = filtered.Where(token => token.SurveyId == surveyId.Value);
        }

        filtered = status switch
        {
            "answered" => filtered.Where(token => token.HasResponse || token.UsedAtUtc.HasValue),
            "ready" => filtered.Where(token => !token.HasResponse && !token.UsedAtUtc.HasValue && (!token.ExpiresAtUtc.HasValue || token.ExpiresAtUtc.Value > DateTimeOffset.UtcNow)),
            "expired" => filtered.Where(token => !token.HasResponse && !token.UsedAtUtc.HasValue && token.ExpiresAtUtc.HasValue && token.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow),
            _ => filtered
        };

        if (!string.IsNullOrWhiteSpace(deliveryStatus))
        {
            filtered = filtered.Where(token => string.Equals(token.DeliveryStatus, deliveryStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(surveyScope, "general", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(token => token.IsGeneralSurvey);
        }
        else if (string.Equals(surveyScope, "targeted", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(token => !token.IsGeneralSurvey);
        }

        return filtered;
    }

    private static string FormatDeliveryStatus(string value)
    {
        return value switch
        {
            "LinkCreated" => "Link oluşturuldu",
            "Sent" => "Gönderildi",
            "Failed" => "Başarısız",
            "NotConfigured" => "Yapılandırılmadı",
            "Legacy" => "Eski link",
            _ => value
        };
    }
}
