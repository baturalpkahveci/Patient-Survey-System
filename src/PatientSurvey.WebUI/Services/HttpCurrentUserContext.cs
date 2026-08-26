using System.Security.Claims;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.WebUI.Services;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string Username
    {
        get
        {
            var identity = _httpContextAccessor.HttpContext?.User.Identity;
            return identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(identity.Name)
                ? identity.Name!
                : "Sistem";
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? RequestPath => _httpContextAccessor.HttpContext?.Request.Path.Value;
}
