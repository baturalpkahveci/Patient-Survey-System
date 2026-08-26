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

    public TokensController(SurveyAccessTokenService tokenService)
    {
        _tokenService = tokenService;
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
            SurveyUrlPrefix = $"{Request.Scheme}://{Request.Host}/Survey/",
            Search = search,
            SurveyId = surveyId,
            Status = status,
            DeliveryStatus = deliveryStatus,
            SurveyScope = surveyScope,
            TotalCount = tokens.Count
        });
    }

    public IActionResult Create()
    {
        return Forbid();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateTokenViewModel viewModel)
    {
        return Forbid();
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
