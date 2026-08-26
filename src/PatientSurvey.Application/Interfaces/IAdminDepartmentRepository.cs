using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IAdminDepartmentRepository
{
    Task<IReadOnlyCollection<Department>> GetAllDepartmentsWithResponsesAsync(CancellationToken cancellationToken);
    Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken);
    Task<bool> DepartmentNameExistsAsync(string name, CancellationToken cancellationToken);
    Task<bool> DepartmentNameExistsForAnotherAsync(int departmentId, string name, CancellationToken cancellationToken);
    void AddDepartment(Department department);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
