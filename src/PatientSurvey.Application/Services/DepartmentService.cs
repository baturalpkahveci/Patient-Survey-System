using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Common;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Services;

public sealed class DepartmentService
{
    private readonly ISurveyReadRepository _repository;
    private readonly IAdminDepartmentRepository _adminRepository;

    public DepartmentService(ISurveyReadRepository repository, IAdminDepartmentRepository adminRepository)
    {
        _repository = repository;
        _adminRepository = adminRepository;
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> GetActiveDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _repository.GetActiveDepartmentsAsync(cancellationToken);
        return departments
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentDto(department.Id, department.Name))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AdminDepartmentListItemDto>> GetAdminDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await _adminRepository.GetAllDepartmentsWithResponsesAsync(cancellationToken);
        return departments
            .OrderBy(department => department.Name)
            .Select(department => new AdminDepartmentListItemDto(
                department.Id,
                department.Name,
                department.IsActive,
                department.SurveyResponses.Count))
            .ToArray();
    }

    public async Task<ServiceResult> CreateDepartmentAsync(
        CreateDepartmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult.Failure("department_name_required", "Bolum adi zorunludur.");
        }

        if (await _adminRepository.DepartmentNameExistsAsync(name, cancellationToken))
        {
            return ServiceResult.Failure("department_exists", "Bu bolum zaten kayitli.");
        }

        _adminRepository.AddDepartment(new Department
        {
            Name = name,
            IsActive = request.IsActive
        });

        await _adminRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ToggleDepartmentStatusAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        var department = await _adminRepository.GetDepartmentByIdAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return ServiceResult.Failure("department_not_found", "Bolum bulunamadi.");
        }

        department.IsActive = !department.IsActive;
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}
