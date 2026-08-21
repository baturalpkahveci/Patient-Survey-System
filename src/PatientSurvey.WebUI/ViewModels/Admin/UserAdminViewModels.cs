using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.User;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class UserIndexViewModel
{
    public IReadOnlyCollection<UserListItemDto> Users { get; set; } = Array.Empty<UserListItemDto>();
}

public sealed class CreateUserViewModel
{
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol secin.")]
    public int? RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public IReadOnlyCollection<RoleOptionDto> Roles { get; set; } = Array.Empty<RoleOptionDto>();
}
