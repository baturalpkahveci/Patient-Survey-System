using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Department;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class DepartmentIndexViewModel
{
    public IReadOnlyCollection<AdminDepartmentListItemDto> Departments { get; set; } = Array.Empty<AdminDepartmentListItemDto>();
    public int? EditingDepartmentId { get; set; }
}

public sealed class CreateDepartmentViewModel
{
    [Required(ErrorMessage = "Bölüm adı zorunludur.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class UpdateDepartmentViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bölüm adı zorunludur.")]
    public string Name { get; set; } = string.Empty;
}
