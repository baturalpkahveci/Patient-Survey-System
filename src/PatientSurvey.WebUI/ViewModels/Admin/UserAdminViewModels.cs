using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.DTOs.User;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class UserIndexViewModel
{
    public IReadOnlyCollection<UserListItemDto> Users { get; set; } = Array.Empty<UserListItemDto>();
    public IReadOnlyCollection<RoleOptionDto> Roles { get; set; } = Array.Empty<RoleOptionDto>();
    public IReadOnlyCollection<DepartmentDto> Departments { get; set; } = Array.Empty<DepartmentDto>();
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public string? Status { get; set; }
    public int? DepartmentId { get; set; }
    public int? EditingUserId { get; set; }
    public int TotalCount { get; set; }
}

public sealed class CreateUserViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçin.")]
    public int? RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public IReadOnlyCollection<RoleOptionDto> Roles { get; set; } = Array.Empty<RoleOptionDto>();
    public IReadOnlyCollection<DepartmentDto> Departments { get; set; } = Array.Empty<DepartmentDto>();
    public string? DoctorFirstName { get; set; }
    public string? DoctorLastName { get; set; }
    public int? DoctorDepartmentId { get; set; }
}

public sealed class UpsertDoctorProfileViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Doktor adı zorunludur.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doktor soyadı zorunludur.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bölüm seçin.")]
    public int? DepartmentId { get; set; }
}
