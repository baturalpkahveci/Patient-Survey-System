using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.User;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class UserIndexViewModel
{
    public IReadOnlyCollection<UserListItemDto> Users { get; set; } = Array.Empty<UserListItemDto>();
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
}
