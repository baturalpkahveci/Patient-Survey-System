using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DepartmentsController : Controller
{
    private readonly DepartmentService _departmentService;

    public DepartmentsController(DepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index(int? editDepartmentId, CancellationToken cancellationToken)
    {
        return View(new DepartmentIndexViewModel
        {
            Departments = await _departmentService.GetAdminDepartmentsAsync(cancellationToken),
            EditingDepartmentId = editDepartmentId
        });
    }

    public IActionResult Create()
    {
        return View(new CreateDepartmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDepartmentViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _departmentService.CreateDepartmentAsync(
            new CreateDepartmentRequestDto(viewModel.Name, viewModel.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Bölüm oluşturulamadı.");
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateDepartmentViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminMessage"] = "Bölüm adı zorunludur.";
            return RedirectToAction(nameof(Index), new { editDepartmentId = viewModel.Id });
        }

        var result = await _departmentService.UpdateDepartmentAsync(
            new UpdateDepartmentRequestDto(viewModel.Id, viewModel.Name),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { editDepartmentId = viewModel.Id });
        }

        TempData["AdminMessage"] = "Bölüm güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var result = await _departmentService.ToggleDepartmentStatusAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
