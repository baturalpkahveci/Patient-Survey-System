using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Doctor;
using PatientSurvey.Application.DTOs.User;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : Controller
{
    private readonly UserService _userService;
    private readonly DoctorService _doctorService;
    private readonly PermissionService _permissionService;

    public UsersController(
        UserService userService,
        DoctorService doctorService,
        PermissionService permissionService)
    {
        _userService = userService;
        _doctorService = doctorService;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? roleId,
        string? status,
        int? departmentId,
        int? editUserId,
        CancellationToken cancellationToken)
    {
        var roles = await _userService.GetRoleOptionsAsync(cancellationToken);
        var permissions = await _permissionService.GetPermissionOptionsAsync(cancellationToken);
        var departments = await _doctorService.GetDepartmentOptionsAsync(cancellationToken);
        var users = await _userService.GetUsersAsync(cancellationToken);
        var filtered = ApplyFilters(users, roles, search, roleId, status, departmentId).ToArray();

        return View(new UserIndexViewModel
        {
            Users = filtered,
            Roles = roles,
            Permissions = permissions,
            Departments = departments,
            Search = search,
            RoleId = roleId,
            Status = status,
            DepartmentId = departmentId,
            EditingUserId = editUserId,
            TotalCount = filtered.Length
        });
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new CreateUserViewModel
        {
            Roles = await _userService.GetRoleOptionsAsync(cancellationToken),
            Permissions = await _permissionService.GetPermissionOptionsAsync(cancellationToken),
            Departments = await _doctorService.GetDepartmentOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel viewModel, CancellationToken cancellationToken)
    {
        var roles = await _userService.GetRoleOptionsAsync(cancellationToken);
        var permissions = await _permissionService.GetPermissionOptionsAsync(cancellationToken);
        var departments = await _doctorService.GetDepartmentOptionsAsync(cancellationToken);
        var selectedRole = viewModel.RoleId.HasValue
            ? roles.FirstOrDefault(role => role.Id == viewModel.RoleId.Value)
            : null;
        var isDoctor = string.Equals(selectedRole?.Name, "Doctor", StringComparison.OrdinalIgnoreCase);

        if (!viewModel.RoleId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.RoleId), "Rol seçin.");
        }

        if (isDoctor)
        {
            if (viewModel.CanViewPatientPersonalData)
            {
                ModelState.AddModelError(
                    nameof(viewModel.CanViewPatientPersonalData),
                    "Doktor rolüne hasta kişisel verisi görüntüleme yetkisi verilemez.");
            }

            if (string.IsNullOrWhiteSpace(viewModel.DoctorFirstName))
            {
                ModelState.AddModelError(nameof(viewModel.DoctorFirstName), "Doktor adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(viewModel.DoctorLastName))
            {
                ModelState.AddModelError(nameof(viewModel.DoctorLastName), "Doktor soyadı zorunludur.");
            }

            if (!viewModel.DoctorDepartmentId.HasValue)
            {
                ModelState.AddModelError(nameof(viewModel.DoctorDepartmentId), "Bölüm seçin.");
            }
        }

        if (!ModelState.IsValid)
        {
            viewModel.Roles = roles;
            viewModel.Permissions = permissions;
            viewModel.Departments = departments;
            return View(viewModel);
        }

        var result = await _userService.CreateUserAndReturnIdAsync(
            new CreateUserRequestDto(
                viewModel.Username,
                viewModel.Password,
                viewModel.RoleId!.Value,
                viewModel.IsActive),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Kullanıcı oluşturulamadı.");
            viewModel.Roles = roles;
            viewModel.Permissions = permissions;
            viewModel.Departments = departments;
            return View(viewModel);
        }

        if (viewModel.CanViewPatientPersonalData)
        {
            var permissionResult = await _permissionService.SetCanViewPatientPersonalDataAsync(
                result.Value,
                canView: true,
                GetCurrentUserId(),
                cancellationToken);

            if (!permissionResult.IsSuccess)
            {
                TempData["AdminMessage"] = $"Kullanıcı oluşturuldu fakat yetki kaydedilemedi: {permissionResult.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        if (isDoctor)
        {
            var doctorResult = await _doctorService.UpsertDoctorProfileAsync(
                new UpsertDoctorProfileRequestDto(
                    result.Value,
                    viewModel.DoctorFirstName ?? string.Empty,
                    viewModel.DoctorLastName ?? string.Empty,
                    viewModel.DoctorDepartmentId.GetValueOrDefault()),
                cancellationToken);

            if (!doctorResult.IsSuccess)
            {
                TempData["AdminMessage"] = $"Kullanıcı oluşturuldu fakat doktor profili kaydedilemedi: {doctorResult.Message}";
                return RedirectToAction(nameof(Index), new { status = "doctor-missing-profile" });
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertDoctorProfile(
        UpsertDoctorProfileViewModel viewModel,
        string? returnSearch,
        int? returnRoleId,
        string? returnStatus,
        int? returnDepartmentId,
        CancellationToken cancellationToken)
    {
        if (!viewModel.DepartmentId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.DepartmentId), "Bölüm seçin.");
        }

        var selectedDepartmentId = viewModel.DepartmentId.GetValueOrDefault();

        if (!ModelState.IsValid)
        {
            TempData["AdminMessage"] = "Doktor bilgileri eksik.";
            return RedirectToAction(nameof(Index), new
            {
                search = returnSearch,
                roleId = returnRoleId,
                status = returnStatus,
                departmentId = returnDepartmentId,
                editUserId = viewModel.UserId
            });
        }

        var result = await _doctorService.UpsertDoctorProfileAsync(
            new UpsertDoctorProfileRequestDto(
                viewModel.UserId,
                viewModel.FirstName,
                viewModel.LastName,
                selectedDepartmentId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new
            {
                search = returnSearch,
                roleId = returnRoleId,
                status = returnStatus,
                departmentId = returnDepartmentId,
                editUserId = viewModel.UserId
            });
        }

        TempData["AdminMessage"] = "Doktor bilgileri kaydedildi.";
        return RedirectToAction(nameof(Index), new
        {
            search = returnSearch,
            roleId = returnRoleId,
            status = returnStatus,
            departmentId = returnDepartmentId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePatientPersonalDataPermission(
        int id,
        bool canViewPatientPersonalData,
        string? returnSearch,
        int? returnRoleId,
        string? returnStatus,
        int? returnDepartmentId,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.SetCanViewPatientPersonalDataAsync(
            id,
            canViewPatientPersonalData,
            GetCurrentUserId(),
            cancellationToken);

        TempData["AdminMessage"] = result.IsSuccess
            ? "Hasta kişisel verisi yetkisi güncellendi."
            : result.Message;

        return RedirectToAction(nameof(Index), new
        {
            search = returnSearch,
            roleId = returnRoleId,
            status = returnStatus,
            departmentId = returnDepartmentId
        });
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

    private static IEnumerable<UserListItemDto> ApplyFilters(
        IReadOnlyCollection<UserListItemDto> users,
        IReadOnlyCollection<RoleOptionDto> roles,
        string? search,
        int? roleId,
        string? status,
        int? departmentId)
    {
        var filtered = users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(user =>
                user.Username.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (user.DoctorFirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (user.DoctorLastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (roleId.HasValue)
        {
            var roleName = roles.FirstOrDefault(role => role.Id == roleId.Value)?.Name;
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                filtered = filtered.Where(user => string.Equals(user.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
            }
        }

        filtered = status switch
        {
            "active" => filtered.Where(user => user.IsActive),
            "inactive" => filtered.Where(user => !user.IsActive),
            "doctor-missing-profile" => filtered.Where(user =>
                string.Equals(user.RoleName, "Doctor", StringComparison.OrdinalIgnoreCase)
                && !user.DoctorId.HasValue),
            _ => filtered
        };

        if (departmentId.HasValue)
        {
            filtered = filtered.Where(user => user.DoctorDepartmentId == departmentId.Value);
        }

        return filtered;
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
