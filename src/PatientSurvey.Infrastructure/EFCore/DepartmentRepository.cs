using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class DepartmentRepository :
    RepositoryBase<Department>,
    IDepartmentRepository
{
    public DepartmentRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
    {
        return await FindByCondition(department => department.IsActive, trackChanges: false)
            .OrderBy(department => department.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Department?> GetOneDepartmentByIdAsync(
        int departmentId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return FindByCondition(department => department.Id == departmentId, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOneDepartment(Department department)
    {
        Create(department);
    }

    public void UpdateOneDepartment(Department department)
    {
        Update(department);
    }

    public void DeleteOneDepartment(Department department)
    {
        Delete(department);
    }
}
