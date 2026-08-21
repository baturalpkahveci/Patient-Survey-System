using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IDepartmentRepository :
    IRepositoryBase<Department>
{
    Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken);
    Task<Department?> GetOneDepartmentByIdAsync(int departmentId, bool trackChanges, CancellationToken cancellationToken);
    void CreateOneDepartment(Department department);
    void UpdateOneDepartment(Department department);
    void DeleteOneDepartment(Department department);
}
