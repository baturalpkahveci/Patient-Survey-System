using System.ComponentModel.DataAnnotations;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.DTOs.Doctor;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class DoctorIndexViewModel
{
    public IReadOnlyCollection<AdminDoctorListItemDto> Doctors { get; set; } = Array.Empty<AdminDoctorListItemDto>();
}

public sealed class EditDoctorDepartmentViewModel
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bölüm seçin.")]
    public int? DepartmentId { get; set; }

    public IReadOnlyCollection<DepartmentDto> Departments { get; set; } = Array.Empty<DepartmentDto>();
}
