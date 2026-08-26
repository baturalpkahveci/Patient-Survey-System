using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.Security;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Account;

namespace PatientSurvey.WebUI.Controllers;

public sealed class AccountController : Controller
{
    private readonly UserService _userService;
    private readonly DoctorService _doctorService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserService userService, DoctorService doctorService, ILogger<AccountController> logger)
    {
        _userService = userService;
        _doctorService = doctorService;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _userService.AuthenticateAsync(viewModel.Username, viewModel.Password, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            _logger.LogWarning("Failed login attempt for username {Username}", viewModel.Username);
            ModelState.AddModelError(string.Empty, result.Message ?? "Kullanıcı adı veya şifre hatalı.");
            return View(viewModel);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.Value.Id.ToString()),
            new(ClaimTypes.Name, result.Value.Username),
            new(ClaimTypes.Role, result.Value.RoleName)
        };

        foreach (var permissionName in result.Value.PermissionNames ?? Array.Empty<string>())
        {
            claims.Add(new Claim(AppPermissionClaimTypes.Permission, permissionName));
        }

        if (string.Equals(result.Value.RoleName, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            var profile = await _doctorService.GetDoctorProfileAsync(result.Value.Id, cancellationToken);
            if (profile.IsSuccess && profile.Value is not null)
            {
                claims.Add(new Claim("doctor_display_name", $"Dr. {profile.Value.FirstName} {profile.Value.LastName}"));
                claims.Add(new Claim("doctor_department_name", profile.Value.DepartmentName));
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrWhiteSpace(viewModel.ReturnUrl) && Url.IsLocalUrl(viewModel.ReturnUrl))
        {
            return Redirect(viewModel.ReturnUrl);
        }

        var area = result.Value.RoleName switch
        {
            "Manager" => "Manager",
            "Doctor" => "Doctor",
            _ => "Admin"
        };

        return RedirectToAction("Index", "Dashboard", new { area });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
