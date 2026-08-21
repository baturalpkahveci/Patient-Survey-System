using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.User;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : Controller
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new UserIndexViewModel
        {
            Users = await _userService.GetUsersAsync(cancellationToken)
        });
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new CreateUserViewModel
        {
            Roles = await _userService.GetRoleOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!viewModel.RoleId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.RoleId), "Rol secin.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Roles = await _userService.GetRoleOptionsAsync(cancellationToken);
            return View(viewModel);
        }

        var result = await _userService.CreateUserAsync(
            new CreateUserRequestDto(
                viewModel.Username,
                viewModel.Password,
                viewModel.RoleId!.Value,
                viewModel.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Kullanici olusturulamadi.");
            viewModel.Roles = await _userService.GetRoleOptionsAsync(cancellationToken);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var result = await _userService.ToggleUserStatusAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
