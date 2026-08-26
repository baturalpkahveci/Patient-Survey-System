using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;
using PatientSurvey.WebUI.ViewModels.Doctor;

namespace PatientSurvey.WebUI.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public sealed class TokensController : Controller
{
    private readonly SurveyAccessTokenService _tokenService;
    private readonly DoctorService _doctorService;

    public TokensController(SurveyAccessTokenService tokenService, DoctorService doctorService)
    {
        _tokenService = tokenService;
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? surveyId,
        string? deliveryStatus,
        CancellationToken cancellationToken)
    {
        var profile = await _doctorService.GetDoctorProfileAsync(GetCurrentUserId(), cancellationToken);
        var doctorId = profile.Value?.Id;
        var departmentId = profile.Value?.DepartmentId;
        var allTokens = await _tokenService.GetTokensAsync(cancellationToken);
        var ownTokens = allTokens
            .Where(token => token.SurveyDoctorId == doctorId && token.SurveyDepartmentId == departmentId)
            .ToArray();
        var filtered = ApplyFilters(ownTokens, search, surveyId, deliveryStatus).ToArray();

        return View(new DoctorTokenIndexViewModel
        {
            Tokens = filtered,
            SurveyOptions = ownTokens
                .GroupBy(token => new { token.SurveyId, token.SurveyTitle })
                .OrderBy(group => group.Key.SurveyTitle)
                .Select(group => new FilterOptionViewModel(group.Key.SurveyId.ToString(), group.Key.SurveyTitle))
                .ToArray(),
            DeliveryOptions = ownTokens
                .Select(token => token.DeliveryStatus)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .Select(value => new FilterOptionViewModel(value, FormatDeliveryStatus(value)))
                .ToArray(),
            SurveyUrlPrefix = $"{Request.Scheme}://{Request.Host}/Survey/",
            Search = search,
            SurveyId = surveyId,
            DeliveryStatus = deliveryStatus,
            TotalCount = ownTokens.Length
        });
    }

    private static IEnumerable<SurveyAccessTokenListItemDto> ApplyFilters(
        IReadOnlyCollection<SurveyAccessTokenListItemDto> tokens,
        string? search,
        int? surveyId,
        string? deliveryStatus)
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

        if (!string.IsNullOrWhiteSpace(deliveryStatus))
        {
            filtered = filtered.Where(token => string.Equals(token.DeliveryStatus, deliveryStatus, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
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
